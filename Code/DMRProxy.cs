namespace DMRWebScrapper_service.Code
{
    using DMRWebScrapper_service.Models;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Net.Mail;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Web;
    using System.Xml.Linq;

    /// <summary>
    /// DMR Proxy som laver opslaget hos Motorregistret og parsing ind i objekt
    /// </summary>
    public class DMRProxy
    {
        /// <summary>
        /// Hent oplysninger fra Motorregister
        /// </summary>
        /// <param name="regnr">nummerplade på køretøj</param>
        /// <param name="dato">historisk dato for oplysninger</param>
        /// <returns></returns>
        public static async Task<Bildata?> HentOplysninger(string regnr, DateTime dato)
        {
            CookieContainer CookieJar = new CookieContainer();
            HttpClientHandler handler = new HttpClientHandler { CookieContainer = CookieJar };
            HttpClient client = new HttpClient(handler);
           
            Bildata bildata = new Bildata();

            var startPage = await client.GetAsync("https://motorregister.skat.dk/dmr-kerne/dk/skat/dmr/front/portlets/koeretoejdetaljer/visKoeretoej/VisKoeretoejController.jpf");

            if (startPage.IsSuccessStatusCode)
            {
                var content = await startPage.Content.ReadAsStringAsync();

                string? tokenElement = Regex.Matches(content, "<input[^>]+?dmrFormToken[^>]+?>", RegexOptions.IgnoreCase)[0].Value;
                if (XElement.Parse(tokenElement)== null)
                {
                    throw new Exception("DmrToken is nullable");
                }
                string token = XElement.Parse(tokenElement).Attribute("value").Value;


                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("dmrFormToken", token),
                    new KeyValuePair<string, string>("wlw-radio_button_group_key:{actionForm.soegekriterie}", "REGISTRERINGSNUMMER"),
                    new KeyValuePair<string, string>("{actionForm.soegeord}", regnr),
                });

                HttpResponseMessage motorInfo1 = await client.PostAsync("https://motorregister.skat.dk/dmr-kerne/dk/skat/dmr/front/portlets/koeretoej/nested/fremsoegKoeretoej/search.do", formContent);
                HttpResponseMessage motorInfo2 = null,
                                    motorInfo3 = null,
                                    motorInfo4 = null,
                                    motorInfo5 = null;

                if (motorInfo1.IsSuccessStatusCode)
                {
                    var motorResult = await motorInfo1.Content.ReadAsStringAsync();

                    // Fail Fast...
                    if (motorResult.Contains("Ingen køretøjer fundet."))
                    {
                        return null;
                    }

                    // Hent historiske data i stedet
                    if (dato.Date < DateTime.Now.Date)
                    {

                        formContent = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("dmrFormToken", token),
                            new KeyValuePair<string, string>("{pageFlow.historiskDato}", dato.ToString("dd-MM-yyyy")),
                            new KeyValuePair<string, string>("HistoriskKnap", "Hent"),
                        });

                        motorInfo1 = await client.PostAsync("https://motorregister.skat.dk/dmr-kerne/dk/skat/dmr/front/portlets/koeretoejdetaljer/nested/visKoeretoej/indstilHistoriskDato.do", formContent);

                        if (motorInfo1.IsSuccessStatusCode)
                        {
                            motorResult = await motorInfo1.Content.ReadAsStringAsync();

                            motorInfo2 = await client.GetAsync("https://motorregister.skat.dk/dmr-front/dmr.portal?_nfpb=true&_windowLabel=kerne_vis_koeretoej&kerne_vis_koeretoej_actionOverride=%2Fdk%2Fskat%2Fdmr%2Ffront%2Fportlets%2Fkoeretoej%2Fnested%2FvisKoeretoej%2FselectTab&kerne_vis_koeretoejdmr_tabset_tab=1&_pageLabel=vis_koeretoej_side");
                            motorInfo3 = await client.GetAsync("https://motorregister.skat.dk/dmr-front/dmr.portal?_nfpb=true&_windowLabel=kerne_vis_koeretoej&kerne_vis_koeretoej_actionOverride=%2Fdk%2Fskat%2Fdmr%2Ffront%2Fportlets%2Fkoeretoej%2Fnested%2FvisKoeretoej%2FselectTab&kerne_vis_koeretoejdmr_tabset_tab=2&_pageLabel=vis_koeretoej_side");
                            motorInfo4 = await client.GetAsync("https://motorregister.skat.dk/dmr-front/dmr.portal?_nfpb=true&_windowLabel=kerne_vis_koeretoej&kerne_vis_koeretoej_actionOverride=%2Fdk%2Fskat%2Fdmr%2Ffront%2Fportlets%2Fkoeretoej%2Fnested%2FvisKoeretoej%2FselectTab&kerne_vis_koeretoejdmr_tabset_tab=3&_pageLabel=vis_koeretoej_side");
                            motorInfo5 = await client.GetAsync("https://motorregister.skat.dk/dmr-front/dmr.portal?_nfpb=true&_windowLabel=kerne_vis_koeretoej&kerne_vis_koeretoej_actionOverride=%2Fdk%2Fskat%2Fdmr%2Ffront%2Fportlets%2Fkoeretoej%2Fnested%2FvisKoeretoej%2FselectTab&kerne_vis_koeretoejdmr_tabset_tab=4&_pageLabel=vis_koeretoej_side");
                        }
                    }
                    else
                    {
                        motorInfo2 = await client.GetAsync("https://motorregister.skat.dk/dmr-kerne/dk/skat/dmr/front/portlets/koeretoejdetaljer/nested/visKoeretoej/selectTab.do?dmr_tabset_tab=1");
                        motorInfo3 = await client.GetAsync("https://motorregister.skat.dk/dmr-kerne/dk/skat/dmr/front/portlets/koeretoejdetaljer/nested/visKoeretoej/selectTab.do?dmr_tabset_tab=2");
                        motorInfo4 = await client.GetAsync("https://motorregister.skat.dk/dmr-kerne/dk/skat/dmr/front/portlets/koeretoejdetaljer/nested/visKoeretoej/selectTab.do?dmr_tabset_tab=3");
                        motorInfo5 = await client.GetAsync("https://motorregister.skat.dk/dmr-kerne/dk/skat/dmr/front/portlets/koeretoejdetaljer/nested/visKoeretoej/selectTab.do?dmr_tabset_tab=4");
                    }

                    ParseKøretøjData(bildata, motorResult);

                    if (motorInfo2.IsSuccessStatusCode)
                    {
                        var MotorResult2 = await motorInfo2.Content.ReadAsStringAsync();

                        ParseTekniskeData(bildata, MotorResult2);
                    }

                    if (motorInfo3.IsSuccessStatusCode)
                    {
                        var MotorResult3 = await motorInfo3.Content.ReadAsStringAsync();

                        ParseSynsData(bildata, MotorResult3);
                    }
                    if (motorInfo4.IsSuccessStatusCode)
                    {
                        var MotorResult4 =await motorInfo4.Content.ReadAsStringAsync();
                        ParseForsikringData(bildata, MotorResult4);

                    }
                    if (motorInfo5.IsSuccessStatusCode)
                    {
                        var MotorResult5 = await motorInfo5.Content.ReadAsStringAsync();

                        ParseAfgiftsData(bildata, MotorResult5);
                    }
                }
            }
            else
            {
                throw new Exception( "Motorregisteret er utilgængeligt");
            }

            return bildata;
        }

        private static void ParseAfgiftsData(Bildata bildata, string MotorResult)
        {
            List<string> tokens = Tokenizer(MotorResult);

            for (int i = 0; i < tokens.Count - 2; i++)
            {
                var value = tokens[i];
                var next = tokens[i + 1];
                var nextnext = tokens[i + 2];

                switch (value)
                {
                    case "Grøn udligningsafgift":
                        bildata.Afgifter.GrønUdligningsafgiftHyppighed = next;
                        bildata.Afgifter.GrønUdligningsafgiftBeløb = nextnext;
                        i++;
                        break;
                    case "Grøn Ejerafgift":
                        bildata.Afgifter.GrønEjerafgiftHyppighed = next;
                        bildata.Afgifter.GrønEjerafgiftBeløb = nextnext;
                        i++;
                        break;
                    case "Sum":
                        bildata.Afgifter.Sum = nextnext;
                        i++;
                        break;

                }
            }
        }

        private static void ParseSynsData(Bildata bildata, string MotorResult)
        {
            List<string> tokens = Tokenizer(MotorResult);

            for (int i = 0; i < tokens.Count - 1; i++)
            {
                var value = tokens[i];
                var next = tokens[i + 1];

                switch (value)
                {
                    case "Resultat:":
                        bildata.Syn.Godkendt = next == "Godkendt";
                        i++;
                        break;
                    case "Dato for syn:":
                        bildata.Syn.SynsDato = DateTime.ParseExact(next, "dd-MM-yyyy", null);
                        i++;
                        break;
                }
            }
        }
        private static void ParseForsikringData(Bildata bildata, string MotorResult)
        {
            List<string> tokens = Tokenizer(MotorResult);


            for (int i = 0; i < tokens.Count - 1; i++)
            {
                var value = tokens[i];
                var next = tokens[i + 1];

                switch (value)
                {
                    case "Selskab:":
                        bildata.Forsikring.Forsikring = next;
                        i++;
                        break;
                    case "Status:":
                        bildata.Forsikring.Aktivt = next.ToLower() == "aktiv";
                        i++;
                        break;
                    case "Oprettet:":
                        bildata.Forsikring.Oprettet = DateTime.ParseExact(next, "dd-MM-yyyy", null);
                        i++;
                        break;
                }
            }
        }
        private static void ParseTekniskeData(Bildata bildata, string MotorResult)
        {
            List<string> tokens = Tokenizer(MotorResult);

            for (int i = 0; i < tokens.Count - 1; i++)
            {
                var value = tokens[i];
                var next = tokens[i + 1];

                switch (value)
                {
                    case "Teknisk totalvægt:":
                        bildata.Teknisk.Vægt.TekniskTotalvægt = ParseInt(next);
                        i++;
                        break;
                    case "Totalvægt:":
                        bildata.Teknisk.Vægt.Totalvægt = ParseInt(next);
                        i++;
                        break;
                    case "Køreklar vægt":
                        if (next == "Minimum:")
                        {
                            next = tokens[i + 2];
                            bildata.Teknisk.Vægt.Minimum = ParseInt(next);
                            i += 2;
                        }
                        break;
                    case "Vogntogsvægt:":
                        bildata.Teknisk.Vægt.Vogntogsvægt = ParseInt(next);
                        i++;
                        break;

                    case "Mærkning:":
                        bildata.Teknisk.Motor.Mærkning = next;
                        i++;
                        break;
                    case "Drivkraft:":
                        bildata.Teknisk.Motor.Drivkraft = next;
                        i++;
                        break;
                    case "Brændstofforbrug:":
                        bildata.Teknisk.Motor.Brændstofforbrug = ParseDouble(next);
                        i++;
                        break;
                    case "Maksimal hastighed:":
                        bildata.Teknisk.Motor.MaksHastighed = ParseDouble(next);
                        i++;
                        break;
                    case "Slagvolumen:":
                        bildata.Teknisk.Motor.Slagvolumen = ParseDouble(next);
                        i++;
                        break;
                    case "Største effekt:":
                        bildata.Teknisk.Motor.Effekt = ParseDouble(next);
                        i++;
                        break;
                    case "Antal cylindre:":
                        bildata.Teknisk.Motor.Cylindre = ParseInt(next);
                        i++;
                        break;

                    case "Karrosseritype:":
                        bildata.Teknisk.Karrosseri.Karrosseritype = next;
                        i++;
                        break;
                    case "Antal døre:":
                        bildata.Teknisk.Karrosseri.AntalDøre = ParseInt(next);
                        i++;
                        break;
                    case "Fælge/dæk:":
                        bildata.Teknisk.Karrosseri.FælgeDæk = next;
                        i++;
                        break;
                    case "Antal siddepladser":
                        if (next == "Minimum:")
                        {
                            var min = ParseInt(tokens[i + 2]);
                            var max = ParseInt(tokens[i + 4]);
                            bildata.Teknisk.Karrosseri.AntalSidepladser = Math.Max(min, max);
                            i += 2;
                        }
                        break;

                    case "CO2-udslip:":
                        bildata.Teknisk.Miljø.CO2 = ParseDouble(next);
                        i++;
                        break;
                    case "Euronorm:":
                        bildata.Teknisk.Miljø.Euronorm = next;
                        i++;
                        break;
                    case "Partikelfilter:":
                        bildata.Teknisk.Miljø.Partikelfilter = next == "Ja";
                        i++;
                        break;
                }
            }
        }

        private static void ParseKøretøjData(Bildata bildata, string MotorResult)
        {
            List<string> tokens = Tokenizer(MotorResult);

            for (int i = 0; i < tokens.Count - 1; i++)
            {
                var value = tokens[i];
                var next = tokens[i + 1];

                switch (value)
                {
                    case "Stelnummer:":

                        bildata.Køretøj.Køretøj.Stelnummer = next;
                        i++;
                        break;
                    case "Mærke, Model, Variant:":
                        bildata.Køretøj.Køretøj.MærkeModel = next;
                        i++;
                        break;
                    case "Art:":
                        bildata.Køretøj.Køretøj.Art = next;
                        i++;
                        break;
                    case "EF-Type-godkendelsenr.:":
                        bildata.Køretøj.Køretøj.EFTypeGodkendelse = next;
                        i++;
                        break;

                    case "Seneste ændring:":
                        if (next.Contains("Registreret"))
                        {
                            bildata.Køretøj.Registreringsforhold.SenesteÆndring = DateTime.ParseExact(next.Replace("Registreret d. ", ""), "dd-MM-yyyy", null);
                            i++;
                        }
                        else if (next.Contains("Afmeldt"))
                        {
                            //throw new Exception("Bilen er Afmeldt");
                        }
                        else
                        {
                            bildata.Køretøj.Køretøj.SenesteÆndring = DateTime.ParseExact(next.Replace("d. ", ""), "dd-MM-yyyy", null);
                            i++;
                        }
                        break;

                    case "Registreringsnummer:":
                        bildata.Køretøj.Registreringsforhold.RegistreringsNummer = next;
                        i++;
                        break;
                    case "Første registrerings&shy;dato:":
                        bildata.Køretøj.Registreringsforhold.FørsteRegistrering = DateTime.ParseExact(next, "dd-MM-yyyy", null);
                        i++;
                        break;
                    case "Anvendelse:":
                        bildata.Køretøj.Registreringsforhold.Anvendelse = next;
                        i++;
                        break;
                    case "Status:":
                        bildata.Køretøj.Registreringsforhold.Status = next;
                        i++;
                        break;
                    case "Type:":
                        bildata.Køretøj.Registreringsforhold.Type = next;
                        i++;
                        break;
                    case "EU-variant:":
                        bildata.Køretøj.Registreringsforhold.EUVariant = next;
                        i++;
                        break;
                    case "EU-version:":
                        bildata.Køretøj.Registreringsforhold.EUVersion = next;
                        i++;
                        break;
                    case "Kategori:":
                        bildata.Køretøj.Registreringsforhold.Kategori = next;
                        i++;
                        break;
                    case "Fabrikant:":
                        bildata.Køretøj.Registreringsforhold.Fabrikat = next;
                        i++;
                        break;

                    case "Farve:":
                        bildata.Køretøj.Identifikation.Farve = next;
                        i++;
                        break;
                    case "Model-år:":
                        bildata.Køretøj.Identifikation.ModelÅr = ParseInt(next);
                        i++;
                        break;
                    case "Bestået NCAP test med mindst 5 stjerner:":
                        bildata.Køretøj.Identifikation.NCAP5 = next == "Ja";
                        i++;
                        break;

                    case "Antal airbags:":
                        bildata.Køretøj.Udstyr.AntalAirbags = ParseInt(next);
                        i++;
                        break;
                    case "Andet udstyr:":
                        bildata.Køretøj.Udstyr.AndetUdstyr = HttpUtility.HtmlDecode(next);
                        i++;
                        break;
                }
            }
        }

        private static List<string> Tokenizer(string MotorResult)
        {
            var values = Regex.Matches(MotorResult, "<[^>]+?>([^<]+)", RegexOptions.Singleline);
            var cleanValues = new List<string>();

            for (int i = 0; i < values.Count - 1; i++)
            {
                var value = values[i].Groups[1].Value.Trim();
                var next = values[i + 1].Groups[1].Value.Trim();

                if (values[i].Groups[1].Value.Trim() != "")
                    cleanValues.Add(values[i].Groups[1].Value.Trim());
            }

            return cleanValues;
        }

        private static int ParseInt(string s)
        {
            int.TryParse(s, out int ret);
            return ret;
        }

        private static double ParseDouble(string s)
        {
            double.TryParse(s.Replace(".", ","), out double ret);
            return ret;
        }
    }
}
