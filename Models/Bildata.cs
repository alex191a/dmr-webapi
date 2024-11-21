namespace DMRWebScrapper_service.Models
{
    public class Bildata
    {
        public DMRKøretøj Køretøj { get; set; }

        public DMRTeknisk Teknisk { get; set; }

        public DMRSyn Syn { get; set; }

        public DMRAfgifter Afgifter { get; set; }

        public DMRForsikring Forsikring { get; set; }
        public Bildata()
        {
            Køretøj = new DMRKøretøj();
            Teknisk = new DMRTeknisk();
            Syn = new DMRSyn();
            Afgifter = new DMRAfgifter();
            Forsikring = new DMRForsikring();
        }
        public bool IsPolice()
        {
            switch (Forsikring.Forsikring.ToUpper())
            {
                //tilføj her efter kommende. hvis flere kriterier tilføjes, kan de bearbejdes her.
                case "SELVFORSIKRING":
                    return true;
                default: return false;

            }
        }
    }
}
