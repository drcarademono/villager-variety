using System;
using System.Collections.Generic;
using System.Reflection;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Utility;    // for NameHelper
using DaggerfallWorkshop.Game.Entity;     // for Genders
using DaggerfallConnect.Utility;          // for DFRandom

namespace VillagerVariety
{
    /// <summary>
    /// Provides custom Morrowind‑style Orc name generation for Orsinium NPCs.
    /// </summary>
    public static class OrsiniumNameHelper
    {
        // Female Orc first names (Morrowind)
        private static readonly string[] femaleFirst = new[] {
            "Agrob","Badbog","Bashuk","Bogdub","Bugdurash","Bula","Bulak","Bulfim","Bum",
            "Burub","Burzob","Dura","Durgat","Durz","Gashnakh","Ghob","Glasha","Glob",
            "Gluronk","Gonk","Grat","Grazob","Gulfim","Kharzug","Lagakh","Lambug","Lazgar",
            "Mogak","Morn","Murob","Murzush","Nargol","Orbul","Ragash","Rolfish","Rulfim",
            "Shadbak","Shagar","Shagdub","Sharn","Sharog","Shelur","Sloomalah","Uloth",
            "Ulumpha","Urzoth","Urzul","Ushug","Yazgash"
        };

        // Male Orc first names (Morrowind)
        private static readonly string[] maleFirst = new[] {
            "Moghakh","Atulg","Azuk","Bagamul","Bakh","Baronk","Bashag","Bazgulub","Bogakh",
            "Bologra","Borug","Both","Bugdul","Bugharz","Bugrash","Bugrol","Bumbub","Burul",
            "Dul","Dular","Duluk","Duma","Dumbuk","Dumburz","Dur","Durbul","Durgash","Durz",
            "Durzol","Durzub","Durzum","Garothmuk","Garzonk","Gashna","Ghamborz","Ghamonk",
            "Ghoragdush","Ghorlorz","Glush","Grat","Gruzgob","Guarg","Gurak","Khadba","Khagra",
            "Khargol","Koffutto","Largakh","Lorbumol","Lorzub","Lugdum","Lugrub","Lurog","Mash",
            "Matuk","Mauhul","Mazorn","Mol","Morbash","Mug","Mugdul","Muk","Murag","Murkub",
            "Murzol","Muzgonk","Nag","Nar","Nash","Ogrul","Ogrumbu","Olfin","Olumba","Orakh",
            "Rogdul","Shakh","Shamar","Shamob","Shargam","Sharkub","Shat","Shazgob","Shulong",
            "Shura","Shurkul","Shuzug","Snaglak","Snakha","Snat","Ugdumph","Ughash","Ulam",
            "Umug","Uram","Urim","Urul","Urzog","Ushamph","Ushat","Yadba","Yagak","Yak",
            "Yam","Yambagorn","Yambul","Yargol","Yashnarz","Yatur"
        };

        // Orc surnames (Morrowind)
        private static readonly string[] surnames = new[] {
            "Agadbu","Aglakh","Agum","Atumph","Azorku","Badbu","Bagrat","Bagul","Bamog","Bar",
            "Bargamph","Bashnag","Bat","Batul","Boga","Bogamakh","Bogharz","Bogla","Boglar",
            "Bogrol","Boguk","Bol","Bolak","Borbog","Borbul","Bug","Bugarn","Bulag","Bularz",
            "Bulfish","Burbug","Burish","Burol","Buzga","Dugul","Dul","Dula","Dulob","Dumul",
            "Dumulg","Durga","Durog","Durug","Dush","Gar","Gashel","Gat","Ghash","Ghasharzol",
            "Gholfim","Gholob","Ghorak","Glorzuf","Gluk","Glurkub","Gorzog","Grambak","Gulfim",
            "Gurakh","Gurub","Kashug","Khagdum","Kharbush","Kharz","Khash","Khashnar","Khatub",
            "Khazor","Lag","Lagdub","Largum","Lazgarn","Loghash","Logob","Logrob","Lorga",
            "Lumbuk","Lumob","Lurkul","Lurn","Luzgan","Magar","Magrish","Mar","Marob","Mashnar",
            "Mogduk","Moghakh","Mughol","Muk","Mulakh","Murgol","Murug","Murz","Muzgob","Muzgub",
            "Muzgur","Ogar","Ogdub","Ogdum","Olor","Olurba","Orbuma","Rimph","Rugob","Rush",
            "Rushub","Shadbuk","Shagdub","Shagdulg","Shagrak","Shagramph","Shak","Sham","Shamub",
            "Sharbag","Sharga","Sharob","Sharolg","Shat","Shatub","Shazog","Shug","Shugarz","Shugham",
            "Shula","Shulor","Shumba","Shuzgub","Skandar","Snagarz","Snagdu","Ufthamph","Uftharz",
            "Ugruma","Ular","Ulfimph","Urgak","Ushar","Ushug","Ushul","Uzgurn","Uzuk","Yagarz",
            "Yak","Yargul","Yarzol"
        };

        /// <summary>
        /// Generates a Morrowind‑style Orc name:
        /// Female: <First> gra-<Surname>
        /// Male:   <First> gro-<Surname>
        /// </summary>
        public static string OrcName(Genders gender)
        {
            var first = (gender == Genders.Female)
                ? femaleFirst[DFRandom.rand() % (uint)femaleFirst.Length]
                : maleFirst[DFRandom.rand() % (uint)maleFirst.Length];

            var surname = surnames[DFRandom.rand() % (uint)surnames.Length];
            var prefix = (gender == Genders.Female) ? "gra-" : "gro-";

            return $"{first} {prefix}{surname}";
        }
    }
}

