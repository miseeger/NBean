using System;
using System.Linq;
using Xunit;

namespace NBean.Tests 
{

    public partial class DatabaseBeanFinderTests : IDisposable
    {
        // https://marcin.gminski.net/blog/generate-realistic-test-data/
        // https://www.generatedata.com

        // https://sqlwatch.io/
        // https://github.com/marcingminski/sqlwatch

        private void CreateTestData()
        {
            _db.Exec(
                "CREATE TABLE Employee (\n  " +
                "    id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,\n  " +
                "    Firstname varchar(255) default NULL,\n  " +
                "    Lastname varchar(255) default NULL,\n  " +
                "    Department varchar(255) default NULL,\n  " +
                "    Phone varchar(100) default NULL,\n  " +
                "    Email varchar(255) default NULL,\n  " +
                "    City varchar(255),\n  " +
                "    StartDate varchar(255)\n  " +
                ")");

            _db.Exec(
                "INSERT INTO Employee(id, Firstname, Lastname, Department, Phone, Email, City, StartDate) VALUES" +
                "(1, \"Rebekah\", \"Sloan\", \"Sales and Marketing\", \"(05735) 3841973\", \"quis@eunibhvulputate.co.uk\", \"Chiniot\", \"2015-02-22T04:40:10-08:00\"), " +
                "(2, \"Baxter\", \"Foley\", \"Asset Management\", \"(034) 10537139\", \"purus.in.molestie@sed.edu\", \"Aisemont\", \"2005-06-22T08:36:02-07:00\"), " +
                "(3, \"Colby\", \"Pratt\", \"Sales and Marketing\", \"(0709) 97008886\", \"Etiam@magnis.edu\", \"Denver\", \"2015-04-02T17:52:01-07:00\"), " +
                "(4, \"Connor\", \"Fowler\", \"Tech Support\", \"(023) 64217915\", \"eleifend.nunc@utsemNulla.edu\", \"Kawawachikamach\", \"2014-07-30T20:08:24-07:00\"), " +
                "(5, \"Nolan\", \"Nolan\", \"Sales and Marketing\", \"(0856) 29817645\", \"erat@idantedictum.org\", \"Nîmes\", \"2001-08-20T08:44:38-07:00\"), " +
                "(6, \"Ifeoma\", \"Pruitt\", \"Tech Support\", \"(078) 38687659\", \"Sed.eu.eros@condimentumDonec.edu\", \"Mundare\", \"2016-01-02T14:13:24-08:00\"), " +
                "(7, \"Shaeleigh\", \"Sanders\", \"Public Relations\", \"(058) 56461330\", \"a.purus@Nulla.co.uk\", \"Bismil\", \"2011-01-15T10:29:52-08:00\"), " +
                "(8, \"Josephine\", \"Lopez\", \"Customer Service\", \"(006) 95317336\", \"mollis.dui.in@imperdietornare.org\", \"Port Coquitlam\", \"2006-08-06T16:33:23-07:00\"), " +
                "(9, \"Derek\", \"Pacheco\", \"Finances\", \"(0543) 92814754\", \"quis.massa@orci.net\", \"Canora\", \"2015-07-31T21:01:18-07:00\"), " +
                "(10, \"Melissa\", \"Cannon\", \"Customer Relations\", \"(0852) 45595613\", \"nascetur@Morbimetus.co.uk\", \"Ahmadnagar\", \"2017-12-31T04:48:04-08:00\");");
            _db.Exec(
                "INSERT INTO Employee (id,Firstname,Lastname,Department,Phone,Email,City,StartDate) VALUES " +
                "(11,\"Marcia\",\"Rice\",\"Customer Relations\",\"(037516) 759317\",\"in.faucibus.orci@nibhsitamet.ca\",\"Stony Plain\",\"2010-12-08T01:19:13-08:00\")," +
                "(12,\"Yael\",\"Welch\",\"Advertising\",\"(008) 52234533\",\"arcu.iaculis@at.com\",\"Penza\",\"2008-07-23T01:12:12-07:00\")," +
                "(13,\"Eden\",\"Montoya\",\"Sales and Marketing\",\"(031390) 385291\",\"sem@semper.com\",\"Bogotá\",\"2006-03-29T22:46:25-08:00\")," +
                "(14,\"Alika\",\"Potts\",\"Media Relations\",\"(07155) 9929320\",\"magna.malesuada.vel@interdumligula.org\",\"Kobbegem\",\"2012-08-01T01:14:01-07:00\")," +
                "(15,\"Kermit\",\"Guerrero\",\"Quality Assurance\",\"(01038) 2728903\",\"nisi.Cum@InloremDonec.com\",\"Bertiolo\",\"2002-09-17T03:54:17-07:00\")," +
                "(16,\"Brittany\",\"Alvarez\",\"Legal Department\",\"(0284) 76670483\",\"tortor@primisinfaucibus.net\",\"Cossignano\",\"2000-11-16T12:15:54-08:00\")," +
                "(17,\"Eve\",\"Cardenas\",\"Advertising\",\"(09466) 5115619\",\"ullamcorper.Duis@sagittis.edu\",\"Jayapura\",\"2001-07-01T22:12:38-07:00\")," +
                "(18,\"Curran\",\"Best\",\"Human Resources\",\"(06611) 8581067\",\"lacinia.Sed@lectusquis.co.uk\",\"Liernu\",\"2016-02-03T19:50:30-08:00\")," +
                "(19,\"Daniel\",\"Justice\",\"Human Resources\",\"(0182) 96669330\",\"Nulla@ipsum.org\",\"Paris\",\"2004-10-05T00:21:29-07:00\")," +
                "(20,\"Keaton\",\"Eaton\",\"Media Relations\",\"(042) 14923912\",\"parturient.montes.nascetur@Sed.com\",\"Calder\",\"2017-01-06T00:16:16-08:00\");");
            _db.Exec(
                "INSERT INTO Employee (id,Firstname,Lastname,Department,Phone,Email,City,StartDate) VALUES " +
                "(21,\"Audrey\",\"Weeks\",\"Finances\",\"(0675) 64227282\",\"viverra.Donec.tempus@ipsumnon.edu\",\"Orhangazi\",\"2001-06-24T17:18:34-07:00\")," +
                "(22,\"Reagan\",\"Malone\",\"Customer Relations\",\"(034553) 224190\",\"Integer.urna.Vivamus@Curae.net\",\"Oudergem\",\"2015-04-23T03:23:23-07:00\")," +
                "(23,\"Aimee\",\"Witt\",\"Payroll\",\"(08591) 3455150\",\"ut.nulla@urna.ca\",\"Logan City\",\"2012-11-25T11:37:29-08:00\")," +
                "(24,\"Nathaniel\",\"Hughes\",\"Media Relations\",\"(0647) 82015626\",\"sem@nonnisi.ca\",\"Coaldale\",\"2021-08-19T02:58:25-07:00\")," +
                "(25,\"Jaime\",\"Austin\",\"Customer Relations\",\"(0328) 66198399\",\"et.netus@condimentumegetvolutpat.edu\",\"Asso\",\"2019-09-22T09:20:25-07:00\")," +
                "(26,\"Kyla\",\"Hewitt\",\"Quality Assurance\",\"(035241) 031152\",\"tellus.Nunc@Loremipsum.org\",\"Didim\",\"2001-12-01T02:08:27-08:00\")," +
                "(27,\"Jamal\",\"Marshall\",\"Sales and Marketing\",\"(093) 68286908\",\"ac@nec.org\",\"Bünyan\",\"2018-09-12T02:04:50-07:00\")," +
                "(28,\"Cally\",\"Pittman\",\"Public Relations\",\"(089) 18855964\",\"tincidunt.tempus.risus@idsapienCras.edu\",\"Pugwash\",\"2006-05-27T07:02:30-07:00\")," +
                "(29,\"Dante\",\"Marquez\",\"Quality Assurance\",\"(02627) 6862023\",\"auctor.quis@dolordapibusgravida.net\",\"Khushab\",\"2000-08-24T00:01:00-07:00\")," +
                "(30,\"Todd\",\"Knox\",\"Payroll\",\"(03241) 9534745\",\"lacinia@ametmassa.co.uk\",\"Quedlinburg\",\"2011-12-24T18:32:11-08:00\");");
            _db.Exec(
                "INSERT INTO Employee (id,Firstname,Lastname,Department,Phone,Email,City,StartDate) VALUES " +
                "(31,\"Helen\",\"Blackwell\",\"Quality Assurance\",\"(079) 74119541\",\"vitae.nibh.Donec@eu.co.uk\",\"Vagli Sotto\",\"2019-11-07T10:14:32-08:00\")," +
                "(32,\"Gage\",\"Pearson\",\"Finances\",\"(0268) 49777697\",\"ante@nuncullamcorper.edu\",\"Latronico\",\"2007-03-22T07:37:12-07:00\")," +
                "(33,\"Autumn\",\"Wilkinson\",\"Quality Assurance\",\"(09918) 5859696\",\"pede.Cum.sociis@ligulaAliquamerat.ca\",\"Navidad\",\"2015-06-29T17:09:14-07:00\")," +
                "(34,\"Elmo\",\"Berry\",\"Media Relations\",\"(02902) 0181037\",\"enim.nisl.elementum@Vivamusmolestiedapibus.com\",\"Port Coquitlam\",\"2021-09-03T19:42:27-07:00\")," +
                "(35,\"Ursa\",\"Larson\",\"Customer Relations\",\"(026) 50737239\",\"ullamcorper.magna.Sed@asollicitudin.co.uk\",\"Banjar\",\"2000-01-29T21:57:21-08:00\")," +
                "(36,\"Lacy\",\"Clements\",\"Research and Development\",\"(033299) 420885\",\"erat.vitae@montesnascetur.org\",\"Aurora\",\"2005-05-19T18:46:30-07:00\")," +
                "(37,\"Melvin\",\"Campbell\",\"Asset Management\",\"(08785) 4374900\",\"ultricies.ornare.elit@semmagnanec.org\",\"Baie-Saint-Paul\",\"2004-04-10T08:48:44-07:00\")," +
                "(38,\"Xander\",\"Dominguez\",\"Research and Development\",\"(0272) 57082231\",\"ut@fermentumconvallisligula.com\",\"Bierges\",\"2018-07-14T04:45:01-07:00\")," +
                "(39,\"Gail\",\"Blackburn\",\"Asset Management\",\"(001) 28369384\",\"luctus.vulputate.nisi@ametrisus.org\",\"Wittenberg\",\"2009-06-19T07:23:24-07:00\")," +
                "(40,\"Timothy\",\"Lowe\",\"Legal Department\",\"(064) 42991096\",\"arcu.Vestibulum.ante@purusDuis.edu\",\"Staraya Kupavna\",\"2016-03-16T21:28:14-07:00\");");
            _db.Exec(
                "INSERT INTO Employee (id,Firstname,Lastname,Department,Phone,Email,City,StartDate) VALUES " +
                "(41,\"Denise\",\"Lucas\",\"Asset Management\",\"(039693) 637259\",\"lorem@nuncQuisqueornare.co.uk\",\"New Bombay\",\"2007-07-12T04:33:24-07:00\")," +
                "(42,\"Trevor\",\"Holden\",\"Legal Department\",\"(0558) 16930454\",\"non.lorem@auctor.net\",\"Elversele\",\"2017-10-13T23:43:59-07:00\")," +
                "(43,\"Hu\",\"Wooten\",\"Accounting\",\"(095) 90652346\",\"pharetra.ut@velitegestas.com\",\"El Bosque\",\"2017-02-03T20:59:32-08:00\")," +
                "(44,\"Patricia\",\"Murphy\",\"Quality Assurance\",\"(07804) 8508273\",\"urna.Nullam@Craslorem.org\",\"Kapuskasing\",\"2018-01-28T02:02:43-08:00\")," +
                "(45,\"Cameron\",\"Ashley\",\"Customer Relations\",\"(0484) 00557210\",\"Donec@nectempusscelerisque.com\",\"Pematangsiantar\",\"2010-01-20T16:21:55-08:00\")," +
                "(46,\"Abraham\",\"Rich\",\"Human Resources\",\"(032589) 456690\",\"sed.pede.Cum@mus.org\",\"Eernegem\",\"2021-04-24T02:11:14-07:00\")," +
                "(47,\"Jaime\",\"Simon\",\"Payroll\",\"(085) 88084707\",\"ut.pharetra@Suspendisse.co.uk\",\"Hofheim am Taunus\",\"2006-07-06T14:29:58-07:00\")," +
                "(48,\"Jared\",\"Blanchard\",\"Tech Support\",\"(0199) 02989105\",\"mi.enim@aptenttaciti.ca\",\"Fort McPherson\",\"2006-01-03T12:29:47-08:00\")," +
                "(49,\"Rogan\",\"Farley\",\"Customer Relations\",\"(037529) 009735\",\"mauris.Morbi.non@justofaucibus.co.uk\",\"Oyace\",\"2002-09-21T20:04:21-07:00\")," +
                "(50,\"Athena\",\"Gregory\",\"Research and Development\",\"(0735) 74011633\",\"orci@magna.org\",\"Salerno\",\"2006-11-29T19:40:50-08:00\");");
            _db.Exec(
                "INSERT INTO Employee (id,Firstname,Lastname,Department,Phone,Email,City,StartDate) VALUES " +
                "(51,\"Casey\",\"Duran\",\"Asset Management\",\"(06818) 1041276\",\"purus.Nullam@Duiscursus.edu\",\"Schwäbisch Gmünd\",\"2015-07-29T10:47:53-07:00\")," +
                "(52,\"Mary\",\"Frederick\",\"Research and Development\",\"(0164) 38535967\",\"dis.parturient@Crasconvallis.org\",\"Tamines\",\"2007-04-02T01:10:16-07:00\")," +
                "(53,\"Thomas\",\"Durham\",\"Quality Assurance\",\"(01806) 3431536\",\"sodales.purus@euarcuMorbi.com\",\"Jackson\",\"2021-10-17T03:09:45-07:00\")," +
                "(54,\"Vladimir\",\"May\",\"Advertising\",\"(08982) 8235786\",\"Phasellus.vitae@nislMaecenasmalesuada.co.uk\",\"College\",\"2008-09-04T16:00:06-07:00\")," +
                "(55,\"Plato\",\"Beach\",\"Quality Assurance\",\"(0366) 05664313\",\"commodo.hendrerit.Donec@diam.org\",\"Bangor\",\"2006-02-17T10:21:20-08:00\")," +
                "(56,\"Mari\",\"Michael\",\"Customer Relations\",\"(09271) 6528457\",\"quis.urna.Nunc@dolorsitamet.net\",\"Auburn\",\"2008-09-12T16:16:53-07:00\")," +
                "(57,\"Whitney\",\"Mercer\",\"Legal Department\",\"(08265) 8809751\",\"quis@turpis.edu\",\"Arviat\",\"2019-07-11T12:35:14-07:00\")," +
                "(58,\"Kyle\",\"Fitzpatrick\",\"Advertising\",\"(059) 74504135\",\"nascetur.ridiculus.mus@enimMaurisquis.net\",\"Monteu Roero\",\"2008-02-09T18:07:15-08:00\")," +
                "(59,\"Xavier\",\"Strong\",\"Advertising\",\"(073) 59289190\",\"tellus.justo@a.com\",\"Colchester\",\"2007-01-26T11:39:58-08:00\")," +
                "(60,\"Brandon\",\"Donovan\",\"Accounting\",\"(09914) 5908120\",\"non.massa@nislMaecenasmalesuada.net\",\"Catanzaro\",\"2006-07-21T00:01:04-07:00\");");
            _db.Exec(
                "INSERT INTO Employee (id,Firstname,Lastname,Department,Phone,Email,City,StartDate) VALUES " +
                "(61,\"Beverly\",\"Kline\",\"Tech Support\",\"(0838) 28957838\",\"justo@nibhDonecest.edu\",\"New Quay\",\"2009-08-25T08:28:21-07:00\")," +
                "(62,\"Ross\",\"Mcdaniel\",\"Customer Service\",\"(00013) 7801683\",\"scelerisque.scelerisque.dui@massaQuisqueporttitor.edu\",\"Castlegar\",\"2018-04-11T21:42:39-07:00\")," +
                "(63,\"Xandra\",\"Johnston\",\"Human Resources\",\"(036) 82637051\",\"sed.dolor@augueeu.net\",\"Stalowa Wola\",\"2017-09-29T09:55:16-07:00\")," +
                "(64,\"Brent\",\"Hays\",\"Accounting\",\"(0093) 26457041\",\"Morbi.accumsan.laoreet@cursusnonegestas.edu\",\"Shikarpur\",\"2014-02-21T00:36:26-08:00\")," +
                "(65,\"Cullen\",\"Franco\",\"Human Resources\",\"(083) 58333920\",\"commodo.tincidunt.nibh@semmollisdui.co.uk\",\"Aisemont\",\"2011-12-14T01:48:27-08:00\")," +
                "(66,\"Tiger\",\"Burnett\",\"Legal Department\",\"(0995) 55141076\",\"dolor.vitae@quamvelsapien.edu\",\"Elen\",\"2018-01-17T03:49:47-08:00\")," +
                "(67,\"Maxine\",\"Bond\",\"Accounting\",\"(0857) 80837811\",\"dui.Cum.sociis@mieleifendegestas.ca\",\"Raipur\",\"2002-12-11T09:16:20-08:00\")," +
                "(68,\"Tamara\",\"Henry\",\"Asset Management\",\"(07330) 3275181\",\"faucibus.id@elitEtiamlaoreet.net\",\"Barry\",\"2018-06-21T07:21:37-07:00\")," +
                "(69,\"Eagan\",\"Mccray\",\"Accounting\",\"(032252) 915352\",\"dis@acmetusvitae.co.uk\",\"Cuernavaca\",\"2009-05-05T04:47:20-07:00\")," +
                "(70,\"Kenneth\",\"Williamson\",\"Public Relations\",\"(039616) 588328\",\"arcu.Sed.et@erat.edu\",\"San José de Alajuela\",\"2002-02-01T22:25:06-08:00\");");
            _db.Exec(
                "INSERT INTO Employee (id,Firstname,Lastname,Department,Phone,Email,City,StartDate) VALUES " +
                "(71,\"Chandler\",\"Joyce\",\"Asset Management\",\"(09595) 0427754\",\"ligula.Nullam@ipsumcursus.ca\",\"Palmerston North\",\"2004-02-03T12:28:11-08:00\")," +
                "(72,\"Carissa\",\"Parsons\",\"Advertising\",\"(0532) 80678776\",\"malesuada.Integer.id@maurisaliquameu.com\",\"South Dum Dum\",\"2004-01-15T04:05:50-08:00\")," +
                "(73,\"Kennedy\",\"Byers\",\"Asset Management\",\"(033476) 749283\",\"a@facilisis.edu\",\"Sesto Campano\",\"2011-08-29T18:19:11-07:00\")," +
                "(74,\"Sasha\",\"Pitts\",\"Advertising\",\"(05298) 9916788\",\"non.quam.Pellentesque@Sednuncest.co.uk\",\"Williams Lake\",\"2001-09-14T14:09:27-07:00\")," +
                "(75,\"Iola\",\"Jarvis\",\"Payroll\",\"(0998) 16307314\",\"justo@Donecdignissim.ca\",\"Upper Hutt\",\"2019-11-13T13:54:41-08:00\")," +
                "(76,\"Ethan\",\"Cooper\",\"Payroll\",\"(039130) 252614\",\"nisl.Quisque.fringilla@Donecelementumlorem.ca\",\"Grembergen\",\"2021-02-05T19:14:47-08:00\")," +
                "(77,\"Tana\",\"Evans\",\"Advertising\",\"(014) 48135806\",\"aliquet.molestie.tellus@justo.ca\",\"Collipulli\",\"2011-02-01T06:10:28-08:00\")," +
                "(78,\"India\",\"Sears\",\"Payroll\",\"(017) 05164386\",\"Phasellus.dapibus@maurisidsapien.com\",\"Ponta Grossa\",\"2005-01-11T14:21:11-08:00\")," +
                "(79,\"Price\",\"Savage\",\"Accounting\",\"(05390) 1342056\",\"neque.pellentesque.massa@lacus.edu\",\"Ansan\",\"2016-09-28T05:54:03-07:00\")," +
                "(80,\"Tarik\",\"Lee\",\"Advertising\",\"(08748) 0591825\",\"dictum.mi.ac@metusIn.net\",\"Floreffe\",\"2005-10-25T04:12:50-07:00\");");
            _db.Exec(
                "INSERT INTO Employee (id,Firstname,Lastname,Department,Phone,Email,City,StartDate) VALUES " +
                "(81,\"Germaine\",\"Oconnor\",\"Public Relations\",\"(08489) 0111488\",\"vulputate.eu@faucibusorci.co.uk\",\"Jupille-sur-Meuse\",\"2010-03-30T09:55:33-07:00\")," +
                "(82,\"Suki\",\"Chan\",\"Research and Development\",\"(0285) 75485195\",\"eu.enim@cursuspurus.com\",\"Freising\",\"2012-03-14T05:27:45-07:00\")," +
                "(83,\"Kaitlin\",\"Cash\",\"Media Relations\",\"(01725) 3465110\",\"ullamcorper.magna@ipsum.ca\",\"Tavistock\",\"2005-10-25T09:44:35-07:00\")," +
                "(84,\"Ulysses\",\"Cash\",\"Customer Relations\",\"(0273) 54914366\",\"magna.nec.quam@tortornibhsit.ca\",\"Richmond Hill\",\"2010-09-13T01:02:49-07:00\")," +
                "(85,\"Carolyn\",\"Rivas\",\"Advertising\",\"(0222) 10068663\",\"nec@rutrumjusto.net\",\"Hafizabad\",\"2018-11-01T20:41:13-07:00\")," +
                "(86,\"Orson\",\"Dejesus\",\"Legal Department\",\"(032832) 838731\",\"aliquam@lacusNullatincidunt.net\",\"Tarcento\",\"2011-12-24T16:05:17-08:00\")," +
                "(87,\"Walker\",\"Contreras\",\"Media Relations\",\"(045) 45687472\",\"et.rutrum@odiosemper.net\",\"Jerez de la Frontera\",\"2006-11-11T15:46:39-08:00\")," +
                "(88,\"Kaden\",\"Goodwin\",\"Media Relations\",\"(0788) 14371791\",\"leo.in@condimentumeget.net\",\"Ayr\",\"2012-03-10T19:53:13-08:00\")," +
                "(89,\"Rafael\",\"Byrd\",\"Asset Management\",\"(012) 89322083\",\"vehicula.risus.Nulla@interdum.net\",\"Balsas\",\"2019-05-22T20:43:36-07:00\")," +
                "(90,\"Helen\",\"Riggs\",\"Finances\",\"(037672) 591456\",\"nisi@a.net\",\"Dunkerque\",\"2020-08-24T13:58:30-07:00\");");
            _db.Exec(
                "INSERT INTO Employee (id,Firstname,Lastname,Department,Phone,Email,City,StartDate) VALUES " +
                "(91,\"Michelle\",\"Acosta\",\"Customer Service\",\"(05309) 2670146\",\"primis@Nunccommodoauctor.org\",\"Cabano\",\"2017-05-31T02:07:01-07:00\")," +
                "(92,\"Hedley\",\"Romero\",\"Public Relations\",\"(03061) 5939623\",\"Pellentesque.habitant@ridiculusmusDonec.edu\",\"Hilo\",\"2008-09-07T00:16:39-07:00\")," +
                "(93,\"Kerry\",\"Farrell\",\"Quality Assurance\",\"(04454) 1147504\",\"eu.euismod.ac@fringillaornare.edu\",\"Ledbury\",\"2008-02-06T13:30:57-08:00\")," +
                "(94,\"Gannon\",\"Meyer\",\"Legal Department\",\"(05347) 0480027\",\"tellus.lorem.eu@erat.com\",\"Regina\",\"2012-04-04T05:20:04-07:00\")," +
                "(95,\"Aidan\",\"Hardin\",\"Asset Management\",\"(04283) 4120813\",\"Pellentesque@Curabiturut.edu\",\"Roma\",\"2015-11-19T06:59:12-08:00\")," +
                "(96,\"Honorato\",\"Mcfadden\",\"Public Relations\",\"(06522) 8252245\",\"rutrum@ataugue.edu\",\"Finspång\",\"2006-10-10T05:24:44-07:00\")," +
                "(97,\"Jelani\",\"Avery\",\"Media Relations\",\"(059) 25831692\",\"est@conubianostraper.net\",\"Sierra Gorda\",\"2009-01-24T16:02:31-08:00\")," +
                "(98,\"Rogan\",\"Vega\",\"Customer Service\",\"(035080) 600006\",\"Cras.dolor@interdumligula.com\",\"Pointe-au-Pic\",\"2006-06-14T11:18:44-07:00\")," +
                "(99,\"Jennifer\",\"Mays\",\"Media Relations\",\"(04008) 3200444\",\"ipsum.primis.in@Namligulaelit.ca\",\"Castellana Sicula\",\"2021-09-07T11:57:01-07:00\")," +
                "(100,\"Alec\",\"Moran\",\"Human Resources\",\"(035035) 950436\",\"neque.Morbi@sit.ca\",\"Cañete\",\"2021-11-01T01:45:55-07:00\");");
        }


        [Fact]
        public void PaginatesWithFullTable()
        {
            CreateTestData();

            Assert.Equal(10, _finder.Paginate(true, "Employee", 0).Count());
            Assert.Equal(10, _finder.Paginate(true, "Employee", 1).Count());
            Assert.Equal(10, _finder.Paginate(true, "Employee", 5).Count());
            Assert.Equal(10, _finder.Paginate(true, "Employee", 10).Count());
            Assert.Equal(10, _finder.Paginate(true, "Employee", 11).Count());

            Assert.Equal(60, _finder.Paginate(true, "Employee", 1, 60).Count());
            Assert.Equal(40, _finder.Paginate(true, "Employee", 2, 60).Count());
            Assert.Equal(40, _finder.Paginate(true, "Employee", 5, 60).Count());

            Assert.Equal(100, _finder.Paginate(true, "Employee", 3, 150).Count());
        }


        [Fact]
        public void PaginatesFilterExpression()
        {
            const string fEx = "WHERE Department = {0}";
            const string dep = "Asset Management";

            CreateTestData();

            Assert.Equal(4, _finder.Paginate(true, "Employee", 0, 4, "StartDate", fEx, dep).Count());
            Assert.Equal(4, _finder.Paginate(true, "Employee", 1, 4, "", fEx, dep).Count());
            Assert.Equal(4, _finder.Paginate(true, "Employee", 2, 4, "StartDate", fEx, dep).Count());
            Assert.Equal(2, _finder.Paginate(true, "Employee", 3, 4, "", fEx, dep).Count());
            Assert.Equal(2, _finder.Paginate(true, "Employee", 5, 4, "StartDate", fEx, dep).Count());
        }


        [Fact]
        public void PaginatesLaravelStyle()
        {
            CreateTestData();

            var page = _finder.LPaginate(true, "Employee", 0);
            Assert.Equal(1, page.CurrentPage);
            Assert.Equal(10, page.PerPage);
            Assert.Equal(100, page.Total);
            Assert.Equal(1, page.From);
            Assert.Equal(10, page.To);
            Assert.Equal(2, page.NextPage);
            Assert.Equal(-1, page.PrevPage);
            Assert.Equal(10, page.LastPage);

            page = _finder.LPaginate(true, "Employee");
            Assert.Equal(1, page.CurrentPage);
            Assert.Equal(10, page.PerPage);
            Assert.Equal(100, page.Total);
            Assert.Equal(1, page.From);
            Assert.Equal(10, page.To);
            Assert.Equal(2, page.NextPage);
            Assert.Equal(-1, page.PrevPage);
            Assert.Equal(10, page.LastPage);

            page = _finder.LPaginate(true, "Employee", 3);
            Assert.Equal(3, page.CurrentPage);
            Assert.Equal(10, page.PerPage);
            Assert.Equal(100, page.Total);
            Assert.Equal(21, page.From);
            Assert.Equal(30, page.To);
            Assert.Equal(4, page.NextPage);
            Assert.Equal(2, page.PrevPage);
            Assert.Equal(10, page.LastPage);

            page = _finder.LPaginate(true, "Employee", 5);
            Assert.Equal(5, page.CurrentPage);
            Assert.Equal(10, page.PerPage);
            Assert.Equal(100, page.Total);
            Assert.Equal(41, page.From);
            Assert.Equal(50, page.To);
            Assert.Equal(6, page.NextPage);
            Assert.Equal(4, page.PrevPage);
            Assert.Equal(10, page.LastPage);

            page = _finder.LPaginate(true, "Employee", 10);
            Assert.Equal(10, page.CurrentPage);
            Assert.Equal(10, page.PerPage);
            Assert.Equal(100, page.Total);
            Assert.Equal(91, page.From);
            Assert.Equal(100, page.To);
            Assert.Equal(-1, page.NextPage);
            Assert.Equal(9, page.PrevPage);
            Assert.Equal(10, page.LastPage);

            page = _finder.LPaginate(true, "Employee", 12, 10, "StartDate");
            Assert.Equal(10, page.CurrentPage);
            Assert.Equal(10, page.PerPage);
            Assert.Equal(100, page.Total);
            Assert.Equal(91, page.From);
            Assert.Equal(100, page.To);
            Assert.Equal(-1, page.NextPage);
            Assert.Equal(9, page.PrevPage);
            Assert.Equal(10, page.LastPage);
        }


        [Fact]
        public void PaginatesLaravelStyleOdd()
        {
            CreateTestData();

            var page = _finder.LPaginate(true, "Employee", 1, 30);
            Assert.Equal(1, page.CurrentPage);
            Assert.Equal(30, page.PerPage);
            Assert.Equal(100, page.Total);
            Assert.Equal(1, page.From);
            Assert.Equal(30, page.To);
            Assert.Equal(2, page.NextPage);
            Assert.Equal(-1, page.PrevPage);
            Assert.Equal(4, page.LastPage);

            page = _finder.LPaginate(true, "Employee", 2, 30);
            Assert.Equal(2, page.CurrentPage);
            Assert.Equal(30, page.PerPage);
            Assert.Equal(100, page.Total);
            Assert.Equal(31, page.From);
            Assert.Equal(60, page.To);
            Assert.Equal(3, page.NextPage);
            Assert.Equal(1, page.PrevPage);
            Assert.Equal(4, page.LastPage);

            page = _finder.LPaginate(true, "Employee", 4, 30);
            Assert.Equal(4, page.CurrentPage);
            Assert.Equal(30, page.PerPage);
            Assert.Equal(100, page.Total);
            Assert.Equal(91, page.From);
            Assert.Equal(100, page.To);
            Assert.Equal(-1, page.NextPage);
            Assert.Equal(3, page.PrevPage);
            Assert.Equal(4, page.LastPage);

            page = _finder.LPaginate(true, "Employee", 7, 30);
            Assert.Equal(4, page.CurrentPage);
            Assert.Equal(30, page.PerPage);
            Assert.Equal(100, page.Total);
            Assert.Equal(91, page.From);
            Assert.Equal(100, page.To);
            Assert.Equal(-1, page.NextPage);
            Assert.Equal(3, page.PrevPage);
            Assert.Equal(4, page.LastPage);
        }


        [Fact]
        public void PaginatesLavavelStyleWithFilterExpression()
        {
            const string fEx = "WHERE Department = {0}";
            const string dep = "Asset Management";

            CreateTestData();

            var page = _finder.LPaginate(true, "Employee", 0, 4, "", fEx, dep);
            Assert.Equal(1, page.CurrentPage);
            Assert.Equal(4, page.PerPage);
            Assert.Equal(10, page.Total);
            Assert.Equal(1, page.From);
            Assert.Equal(4, page.To);
            Assert.Equal(2, page.NextPage);
            Assert.Equal(-1, page.PrevPage);
            Assert.Equal(3, page.LastPage);

            page = _finder.LPaginate(true, "Employee", 2, 4, "", fEx, dep);
            Assert.Equal(2, page.CurrentPage);
            Assert.Equal(4, page.PerPage);
            Assert.Equal(10, page.Total);
            Assert.Equal(5, page.From);
            Assert.Equal(8, page.To);
            Assert.Equal(3, page.NextPage);
            Assert.Equal(1, page.PrevPage);
            Assert.Equal(3, page.LastPage);

            page = _finder.LPaginate(true, "Employee", 3, 4, "", fEx, dep);
            Assert.Equal(3, page.CurrentPage);
            Assert.Equal(4, page.PerPage);
            Assert.Equal(10, page.Total);
            Assert.Equal(9, page.From);
            Assert.Equal(10, page.To);
            Assert.Equal(-1, page.NextPage);
            Assert.Equal(2, page.PrevPage);
            Assert.Equal(3, page.LastPage);
        }


        [Fact]
        public void PaginatesLaravelStyleWithIgnorelist()
        {
            CreateTestData();

            var page = _finder.LPaginate(true, "Employee", 1, 5, "FirstEmail,StartDate,Phone,City");
            Assert.Equal(5, page.Data.Length);
            Assert.Equal(5, page.Data[0].Keys.Count); 
            Assert.True(page.Data[0].Keys.Contains("Email"));
            Assert.False(page.Data[0].Keys.Contains("StartDate"));
            Assert.False(page.Data[0].Keys.Contains("Phone"));
            Assert.False(page.Data[0].Keys.Contains("City"));
        }


        [Fact]
        public void PaginatesLaravelStyleToJson()
        {
            CreateTestData();

            Assert.Equal(
                "{\"data\":[{\"id\":4,\"firstname\":\"Connor\",\"lastname\":\"Fowler\"," +
                "\"department\":\"Tech Support\",\"phone\":\"(023) 64217915\"," +
                "\"email\":\"eleifend.nunc@utsemNulla.edu\",\"city\":\"Kawawachikamach\"," +
                "\"startDate\":\"2014-07-30T20:08:24-07:00\"}," +
                "{\"id\":5,\"firstname\":\"Nolan\",\"lastname\":\"Nolan\"," +
                "\"department\":\"Sales and Marketing\",\"phone\":\"(0856) 29817645\"," +
                "\"email\":\"erat@idantedictum.org\",\"city\":\"N\\u00EEmes\"," +
                "\"startDate\":\"2001-08-20T08:44:38-07:00\"}," +
                "{\"id\":6,\"firstname\":\"Ifeoma\",\"lastname\":\"Pruitt\"," +
                "\"department\":\"Tech Support\",\"phone\":\"(078) 38687659\"," +
                "\"email\":\"Sed.eu.eros@condimentumDonec.edu\",\"city\":\"Mundare\"," +
                "\"startDate\":\"2016-01-02T14:13:24-08:00\"}]," +
                "\"total\":100,\"perPage\":3,\"currentPage\":2,\"lastPage\":34,\"nextPage\":3," +
                "\"prevPage\":1,\"from\":4,\"to\":6}",
                _finder.LPaginate(true, "Employee", 2, 3).ToJson());
        }


        [Fact]
        public void PaginatesCustomBeansLaravelStyleToJson()
        {
            CreateTestData();

            Assert.Equal(
                "{\"data\":[{\"columns\":[\"id\",\"Firstname\",\"Lastname\",\"Department\",\"Phone\",\"Email\"," +
                "\"City\",\"StartDate\"],\"data\":{\"id\":4,\"firstname\":\"Connor\",\"lastname\":\"Fowler\"," +
                "\"department\":\"Tech Support\",\"phone\":\"(023) 64217915\",\"email\":\"eleifend.nunc@utsemNulla.edu\"," +
                "\"city\":\"Kawawachikamach\",\"startDate\":\"2014-07-30T20:08:24-07:00\"}},{\"columns\":[\"id\"," +
                "\"Firstname\",\"Lastname\",\"Department\",\"Phone\",\"Email\",\"City\",\"StartDate\"]," +
                "\"data\":{\"id\":5,\"firstname\":\"Nolan\",\"lastname\":\"Nolan\",\"department\":\"Sales and Marketing\"," +
                "\"phone\":\"(0856) 29817645\",\"email\":\"erat@idantedictum.org\",\"city\":\"N\\u00EEmes\"," +
                "\"startDate\":\"2001-08-20T08:44:38-07:00\"}},{\"columns\":[\"id\",\"Firstname\",\"Lastname\"," +
                "\"Department\",\"Phone\",\"Email\",\"City\",\"StartDate\"],\"data\":{\"id\":6,\"firstname\":\"Ifeoma\"," +
                "\"lastname\":\"Pruitt\",\"department\":\"Tech Support\",\"phone\":\"(078) 38687659\"," +
                "\"email\":\"Sed.eu.eros@condimentumDonec.edu\",\"city\":\"Mundare\"," +
                "\"startDate\":\"2016-01-02T14:13:24-08:00\"}}],\"total\":100,\"perPage\":3,\"currentPage\":2," +
                "\"lastPage\":34,\"nextPage\":3,\"prevPage\":1,\"from\":4,\"to\":6}",

                _finder.LPaginate<Employee>(true, 2, 3).ToJson());
        }

    }


    internal class Employee : Bean
    {
        public Employee()
            : base("Employee")
        {
        }
    }

}
