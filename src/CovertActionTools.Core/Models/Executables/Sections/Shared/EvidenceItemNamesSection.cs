using System.Linq;

namespace CovertActionTools.Core.Models.Executables.Sections.Shared
{
    /// <summary>
    /// 65 evidence item name templates: 16 cars, 8 weapons (paired with the gun
    /// rack ordering), 8 addresses, 16 airline tickets, 8 telegrams/teletypes,
    /// 16 cash placeholders ("$"), and 8 ID/passport templates. The trailing
    /// 1-byte empty slot is preserved byte-for-byte from the original data.
    /// </summary>
    public class EvidenceItemNamesSection : ExactCountFixedSizeStringTableSection
    {
        public const int ItemCount = 65;

        protected override int[] StringSizes => new[]
        {
            14, // "Ford Escort #"
            13, // "Chevy Nova #"
            16, // "Toyota Tercel #"
            10, // "VW Golf #"
            12, // "Mazda RX7 #"
            14, // "Honda Civic #"
            9,  // "Peugot #"
            11, // "Chrysler #"
            14, // "Walther PPK #"
            19, // "Smith&Wesson 9mm #"
            15, // "Berreta M92S #"
            15, // "Browning 9mm #"
            18, // "Hechler&Koch VP #"
            12, // "Luger 9mm #"
            10, // "Colt 45 #"
            13, // "357 Magnum #"
            13, // "Rue Laverne "
            13, // "HochStrasse "
            10, // "Main St. "
            12, // "Webley Ct. "
            14, // "Holmley Park "
            13, // "ObensGrabbe "
            14, // "La Paz Blvd. "
            12, // "North Ave. "
            8,  // "PanAm #"
            9,  // "United #"
            9,  // "Sabena #"
            12, // "Lufthansa #"
            8,  // "El Al #"
            6,  // "KLM #"
            11, // "SwissAir #"
            7,  // "BOAC #"
            10, // "W.Union #"
            6,  // "RCA #"
            8,  // "Telex #"
            8,  // "AmTel #"
            11, // "InterTel #"
            10, // "SatComm #"
            10, // "TelTron #"
            10, // "ComLink #"
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, // "$" x 16
            17, // "Swiss Passport #"
            13, // "InterPol ID#"
            20, // "Bahamian Passport #"
            12, // "Gold Card #"
            19, // "Swedish Passport #"
            13, // "Green Card #"
            13, // "VISTA Card #"
            20, // "Liberian Passport #"
            1,  // trailing empty slot
        };

        public EvidenceItemNamesSection Clone()
        {
            return new EvidenceItemNamesSection { Strings = Strings.ToList() };
        }
    }
}
