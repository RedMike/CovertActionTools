namespace CovertActionTools.Core.Models.Executables.Sections
{
    public interface IExecutableSection
    {
        /// <summary>
        /// Should be true only if the section makes sense to view
        /// </summary>
        /// <returns></returns>
        bool Viewable();
        /// <summary>
        /// Should be true only if the section can be usefully edited
        /// </summary>
        /// <returns></returns>
        bool Editable();
        /// <summary>
        /// Reads the section data into the local fields, and returns the size of the section to move the offset
        /// TODO: things that read multiple areas
        /// </summary>
        /// <param name="fullPayload"></param>
        /// <param name="startingOffset"></param>
        /// <returns></returns>
        int ReadBytes(byte[] fullPayload, int startingOffset);
        /// <summary>
        /// Writes the section data into a single byte array
        /// TODO: things that need to write fix-ups/etc
        /// </summary>
        /// <returns></returns>
        byte[] WriteBytes();
    }
}