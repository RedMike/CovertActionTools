"""Parse TRX test results, write a job summary, and comment on the PR on failure."""

import glob
import os
import json
import re
import urllib.request
import xml.etree.ElementTree as ET

TRX_NS = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"
COMMENT_MARKER = "<!-- test-results -->"


def parse_trx_files(results_dir):
    """Parse all TRX files and return (total, passed, failed, skipped, failures)."""
    total = 0
    passed = 0
    failed = 0
    skipped = 0
    failures = []

    for trx_path in glob.glob(os.path.join(results_dir, "**", "*.trx"), recursive=True):
        tree = ET.parse(trx_path)
        root = tree.getroot()

        # Build map of test ID -> (class name, method name)
        test_info = {}
        for test_def in root.iter(f"{{{TRX_NS}}}UnitTest"):
            test_id = test_def.get("id", "")
            method_el = test_def.find(f"{{{TRX_NS}}}TestMethod")
            if method_el is not None:
                class_name = method_el.get("className", "")
                method_name = method_el.get("name", "")
                test_info[test_id] = (class_name, method_name)

        for result in root.iter(f"{{{TRX_NS}}}UnitTestResult"):
            outcome = result.get("outcome", "").lower()
            total += 1
            if outcome == "passed":
                passed += 1
            elif outcome == "failed":
                failed += 1
                test_id = result.get("testId", "")
                test_name = result.get("testName", "")
                class_name, method_name = test_info.get(test_id, ("", test_name))

                # Extract file and line from stack trace
                file_name = ""
                line_number = ""
                error_message = ""

                output_el = result.find(f"{{{TRX_NS}}}Output")
                if output_el is not None:
                    err_el = output_el.find(f"{{{TRX_NS}}}ErrorInfo")
                    if err_el is not None:
                        msg_el = err_el.find(f"{{{TRX_NS}}}Message")
                        if msg_el is not None and msg_el.text:
                            error_message = msg_el.text.strip().split("\n")[0]

                        stack_el = err_el.find(f"{{{TRX_NS}}}StackTrace")
                        if stack_el is not None and stack_el.text:
                            match = re.search(
                                r"in\s+(.+?):line\s+(\d+)", stack_el.text
                            )
                            if match:
                                file_name = os.path.basename(match.group(1))
                                line_number = match.group(2)

                failures.append(
                    {
                        "class": class_name,
                        "method": method_name,
                        "file": file_name,
                        "line": line_number,
                        "message": error_message,
                    }
                )
            else:
                skipped += 1

    return total, passed, failed, skipped, failures


def write_summary(total, passed, failed, skipped, failures):
    """Write markdown test summary to GITHUB_STEP_SUMMARY."""
    summary_path = os.environ.get("GITHUB_STEP_SUMMARY", "")
    if not summary_path:
        return

    lines = ["## Test Results\n"]
    lines.append(f"| Total | Passed | Failed | Skipped |")
    lines.append(f"|-------|--------|--------|---------|")
    lines.append(f"| {total} | {passed} | {failed} | {skipped} |\n")

    if failures:
        lines.append("### Failures\n")
        for f in failures:
            location = f["file"]
            if location and f["line"]:
                location = f"{f['file']}:{f['line']}"
            header = f"**{f['method']}** ({location})" if location else f"**{f['method']}**"
            lines.append(f"- {header}")
            if f["message"]:
                lines.append(f"  > {f['message']}")

    with open(summary_path, "a") as fh:
        fh.write("\n".join(lines) + "\n")


def api_request(url, method="GET", data=None):
    """Make an API request with the GITHUB_TOKEN."""
    token = os.environ.get("GITHUB_TOKEN", "")
    headers = {
        "Authorization": f"token {token}",
        "Accept": "application/json",
        "Content-Type": "application/json",
    }
    body = json.dumps(data).encode() if data else None
    req = urllib.request.Request(url, data=body, headers=headers, method=method)
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode())


def update_pr_comment(failed, failures):
    """Create or update the test-results PR comment."""
    api_url = os.environ.get("GITHUB_API_URL", "")
    repo = os.environ.get("GITHUB_REPOSITORY", "")
    pr_number = os.environ.get("PR_NUMBER", "")

    if not all([api_url, repo, pr_number]):
        print("Missing environment variables for PR comment, skipping.")
        return

    comments_url = f"{api_url}/repos/{repo}/issues/{pr_number}/comments"

    # Find existing comment
    existing_id = None
    try:
        comments = api_request(comments_url)
        for comment in comments:
            if COMMENT_MARKER in comment.get("body", ""):
                existing_id = comment["id"]
                break
    except Exception as e:
        print(f"Warning: could not list PR comments: {e}")

    if failed > 0:
        lines = [COMMENT_MARKER, "## Test Failures\n"]
        lines.append("| Test | File | Line | Message |")
        lines.append("|------|------|------|---------|")
        for f in failures:
            file_col = f["file"] or "-"
            line_col = f["line"] or "-"
            msg = f["message"].replace("|", "\\|") if f["message"] else "-"
            lines.append(f"| `{f['method']}` | `{file_col}` | {line_col} | {msg} |")
        body = "\n".join(lines)
    else:
        # Tests pass now — update comment to reflect that
        if existing_id is None:
            return  # No previous failure comment, nothing to do
        body = f"{COMMENT_MARKER}\n## Tests Passing\n\nAll tests are passing now."

    try:
        if existing_id:
            update_url = f"{api_url}/repos/{repo}/issues/comments/{existing_id}"
            api_request(update_url, method="PATCH", data={"body": body})
            print(f"Updated existing PR comment {existing_id}.")
        elif failed > 0:
            api_request(comments_url, method="POST", data={"body": body})
            print("Created new PR comment with test failures.")
    except Exception as e:
        print(f"Warning: could not update PR comment: {e}")


def main():
    results_dir = os.path.join(os.environ.get("GITHUB_WORKSPACE", "."), "test-results")
    test_outcome = os.environ.get("TEST_OUTCOME", "")

    if not os.path.isdir(results_dir):
        print(f"No test results directory found at {results_dir}")
        return

    total, passed, failed, skipped, failures = parse_trx_files(results_dir)

    if total == 0:
        print("No test results found in TRX files.")
        return

    print(f"Tests: {total} total, {passed} passed, {failed} failed, {skipped} skipped")

    write_summary(total, passed, failed, skipped, failures)
    update_pr_comment(failed, failures)


if __name__ == "__main__":
    main()
