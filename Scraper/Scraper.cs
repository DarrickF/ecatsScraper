using System;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using IniParser;
using IniParser.Model;
using Newtonsoft.Json.Linq;
using Npgsql;
using System.Configuration;
using Microsoft.Win32.SafeHandles;
using static Scraper.Record;

namespace Scraper
{
    class Scraper
    {




        static void Main(string[] args)
        {

            IWebDriver driver;
            WebDriverWait wait;
            int i = 0, countdown = 800;                                          //used for iterating through different reports, and countdown to relaunch
            string phone = "";

            string chromedrive = "C:\\google_driver\\";

            System.Environment.SetEnvironmentVariable("webdriver.chrome.driver", chromedrive);

            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--user-data-dir=C:\\scraper_cache");
            options.AddArgument("--headless");
            options.AddArgument("--silent");

            using (driver = new ChromeDriver(chromedrive, options))
            {
                wait = new WebDriverWait(driver, TimeSpan.FromSeconds(90));
                Console.WriteLine("LOGGING INTO ECATS");
                //navigating to ECaTS
                driver.Navigate().GoToUrl(@"https://tx.ecats911.com/");

                //Verifying that we have arrived to the desired website
                Console.WriteLine("CHECKING WEBSITE TITLE");
                wait.Until<bool>((d) => { return d.Title.Contains("User Login"); });
                Console.WriteLine(("ARRIVED"));

                //finding and selecting the element that contains the username box
                IWebElement searchbox = driver.FindElement(By.Id("dnn_ctr_Login_Login_DNN_txtUsername"));
                searchbox.Click();
                //entering the username and pass
                searchbox.SendKeys("dfuentes");
                searchbox = driver.FindElement(By.Id("dnn_ctr_Login_Login_DNN_txtPassword"));
                searchbox.Click();
                searchbox.SendKeys("3K1tten$");
                //searchbox = driver.FindElement(By.Id("dnn_ctr_Login_chkCookie"));
                searchbox.Click();
                searchbox = driver.FindElement(By.Id("dnn_ctr_Login_Login_DNN_cmdLogin"));
                searchbox.Click();


                Console.WriteLine("TAKING A BREAK");
                Thread.Sleep(2000);



                
                  for (; countdown > 0; countdown--)    
                  {
                    Console.WriteLine("SELECTING WHICH REPORT TO RUN " + i + " " + countdown);
                    searchbox = driver.FindElement(By.Id("dnn_ecatsMasthead_imgAdHoc"));
                    searchbox.Click();
                    Console.WriteLine("AD HOC PAGE");

                    if (i == 0)
                    {
                        searchbox = driver.FindElement(By.Id("dnn_ctr375_AdHocReports_rptReportList_ctl04_uxEditReportButton"));
                        searchbox.Click();
                        Console.WriteLine("EMS MEDCARE REPORT");
                        phone = "956-688-6575";
                        i++;
                    }
                    else if (i == 1)
                    {
                        searchbox = driver.FindElement(By.Id("dnn_ctr375_AdHocReports_rptReportList_ctl03_uxEditReportButton"));
                        searchbox.Click();
                        Console.WriteLine("EMS HIDALGO COUNTY REPORT");
                        phone = "956-686-1224";
                        i++;
                    }
                    else if (i == 2)
                    {
                        searchbox = driver.FindElement(By.Id("dnn_ctr375_AdHocReports_rptReportList_ctl02_uxEditReportButton"));
                        searchbox.Click();
                        Console.WriteLine("DPS REPORT");
                        phone = "956-565-7600";
                        i++;
                    }
                    else
                    {
                        searchbox = driver.FindElement(By.Id("dnn_ctr375_AdHocReports_rptReportList_ctl01_uxEditReportButton"));
                        searchbox.Click();
                        Console.WriteLine("FIRE REPORT");
                        phone = "956-681-2525";
                        i = 0;

                    }
                    
                    //Console.WriteLine("PRE NEW TAB WINDOW = " + driver.CurrentWindowHandle.ToString());

                    Console.WriteLine("ENTERING TIME AND DATE");
                    searchbox = driver.FindElement(By.Id("dnn_ctr375_AdHocReports_uxAdvanced_txtStartDate"));
                    searchbox.Clear();
                    DateTime dateTime = DateTime.Now;
                    searchbox.SendKeys(dateTime.ToString("d") + " 00:00:00");

                    searchbox = driver.FindElement(By.Id("dnn_ctr375_AdHocReports_uxAdvanced_txtEndDate"));
                    searchbox.Clear();
                    searchbox.SendKeys(dateTime.ToString("d") + " 23:59:59");

                    searchbox = driver.FindElement(By.Id("dnn_ctr375_AdHocReports_uxAdvanced_uxGenerateButton"));
                    searchbox.Click();


                    Thread.Sleep(5000);
                   // Console.WriteLine(driver.WindowHandles.Count);
                    string originalWindow = driver.CurrentWindowHandle;

                    foreach(string window in driver.WindowHandles)
                    {
                        if(originalWindow != window)
                        {
                            driver.SwitchTo().Window(window);
                            break;
                        }
                    }

                    //wait.Until(wd => wd.Title.Contains("Ad Hoc Report:"));       

                    Console.WriteLine("CHECKING WEBSITE TITLE");

                    //Console.WriteLine(driver.CurrentWindowHandle.ToString());
                    try
                    {
                        wait.Until<bool>((d) => { return d.Title.Contains("Ad Hoc Report: "); });
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine("BREAKOUT: REPORT HANGING");
                        driver.Close();
                        driver.Quit();
                        
                        break;
                    }


                    Console.WriteLine(("ARRIVED"));

                    //below three lines dump data 
                    //var data = driver.FindElements(By.ClassName("reportTable"));
                    //foreach (var test in data)
                    //    Console.WriteLine(data);

                    IReadOnlyCollection<IWebElement> data = waitAndFetchData(driver);
                    List<Record> finalData = getDataFromReport(driver, data);

                    if (finalData.Count > 0)
                    {
                        Console.WriteLine("SAVING DATA");
                        saveToDatabase(finalData, phone);
                    }

                    driver.Close();
                    driver.SwitchTo().Window(originalWindow);


                }
                //if the program gets to this point either the countdown ran out or ECaTS got stuck generating a report
                //the program will run forever unless force closed in task manager
                Console.WriteLine("BREAKOUT: COUNTDOWN ELAPSED");
                System.Diagnostics.Process.Start("Scraper.exe");
                Environment.Exit(0);

            }

        }

        public static IReadOnlyCollection<IWebElement> waitAndFetchData(IWebDriver web)
        {
            Console.WriteLine("LAUNCHED waitAndFetchData");
            IReadOnlyCollection<IWebElement> tables = null;

            bool notFound = true;
            while (notFound)
            {
                Thread.Sleep(1000);
                tables = web.FindElements(By.ClassName("reportTable"));
                Console.WriteLine(tables.Count.ToString());
                //Console.Write("WAITING..");//Console.WriteLine("SIZE OF REPORT TABLES: " + tables.Count.ToString());
                if (tables.Count > 0)
                {
                    notFound = false;
                }
            }
            Console.WriteLine("");                          //cleanup
            return tables;

        }


        public static List<Record> getDataFromReport(IWebDriver web, IReadOnlyCollection<IWebElement> tables)
        {
            Console.WriteLine("LAUNCHED getDataFromReport");
            IWebElement row;
            List<string> holdData = new List<string>();
            List<Record> finalData = new List<Record>();
            int index = 0;
            int mod = 0;

            for (int i = 0; i < tables.Count; i++)
            {
                holdData.Clear();
                IWebElement table = tables.ElementAt(i);

                IReadOnlyCollection<IWebElement> rows = table.FindElements(By.TagName("td"));
                //displays data about what is being scraped. Not necessary unless debugging
                //Console.WriteLine("TOTAL ROWS: " + rows.Count);
                index = 0;
                mod = 0;
                string rowValue = "";

                for (int x = 0; x < rows.Count(); x++)
                {
                    row = rows.ElementAt(x);
                    index = x + 1;
                    mod = index % 11;
                    rowValue = row.Text;
                    if (rowValue.ToLower().IndexOf("no records found") == -1)
                    {
                        if (mod != 0)
                        {
                            //displays data that was scraped. Not necessary unless debugging
                            //Console.WriteLine(rowValue);
                           // Console.WriteLine("");
                            holdData.Add(rowValue);

                        }

                        else if (mod == 0)
                        {
                            holdData.Add(rowValue);
                            holdData.Add(holdData[0]);

                            Record placeholder = new Record(holdData[0], holdData[1], holdData[2], holdData[3], holdData[4], holdData[5], holdData[6], holdData[7],
                                holdData[8], holdData[9], holdData[10], holdData[11]);

                            finalData.Add(placeholder);

                            holdData = new List<string>();
                        }
                    }
                }
            }

            return finalData;
        }

        private static async void saveToDatabase(List<Record> finalData, string phone)
        {
            //Console.WriteLine("SIZE OF COUNT " + finalData.Count.ToString());


            //var connString = "Host=spartandb.lrgvdc911.org;Username=postgres;Password=pgadmin;Database=webapp;";\

            //var connString = string.Format("Host={0};Username={1};Database={2}",
            //   "spartandb.lrgvdc911.org", "postgres", "webapp");

            //await using var conn = new NpgsqlConnection(connString);

            

            NpgsqlConnection conn = new NpgsqlConnection("Server=spartandb.lrgvdc911.org;Port=5432;Database=webapp; User Id=postgres;Password=pgadmin;");
            conn.Open();
            Console.WriteLine("CONNECTING TO DATABASE");
            //await conn.OpenAsync();
            //VERIFICATION
            if (conn.State == ConnectionState.Open)
                Console.WriteLine("CONNECTION ESTABLISHED");

            Console.WriteLine("DELETE ALL SQL");

            var cmd = new NpgsqlCommand("DELETE FROM ali.spill WHERE transfer_number LIKE @s ", conn);
            cmd.Parameters.AddWithValue("s", "%" + phone + "%");
            await cmd.ExecuteNonQueryAsync();



            Console.WriteLine("INSERT SQL");
            var sb = new StringBuilder("INSERT INTO ali.spill VALUES");

            using var command = new NpgsqlCommand(connection: conn, cmdText: null);

            for (int i = 0; i < finalData.Count; i++)
            {
                Record final = finalData[i]; ;

                if (i != 0) sb.Append(",");
                var dName = (i * 12 + 1).ToString();
                var phName = (i * 12 + 2).ToString();
                var ltName = (i * 12 + 3).ToString();
                var lnName = (i * 12 + 4).ToString();
                var aniName = (i * 12 + 5).ToString();
                var addName = (i * 12 + 6).ToString();
                var blname = (i * 12 + 7).ToString();
                var ctName = (i * 12 + 8).ToString();
                var psName = (i * 12 + 9).ToString();
                var tpName = (i * 12 + 10).ToString();
                var prName = (i * 12 + 11).ToString();
                var dtName = (i * 12 + 12).ToString();

                sb.Append("(@").Append(dName).Append(", @").Append(phName).Append(", @").Append(ltName).Append(", @").Append(lnName).Append(", @")
                    .Append(aniName).Append(", @").Append(addName).Append(", @").Append(blname).Append(", @").Append(ctName).Append(", @").Append(psName)
                    .Append(", @").Append(tpName).Append(", @").Append(prName).Append(", @").Append(dtName).Append(")");

                DateTime dTime = DateTime.Parse(final.dName);
                command.Parameters.Add(new NpgsqlParameter<DateTime>(dName, dTime));
                command.Parameters.Add(new NpgsqlParameter<string>(phName, final.phName));
                command.Parameters.Add(new NpgsqlParameter<string>(ltName, final.ltName));
                command.Parameters.Add(new NpgsqlParameter<string>(lnName, final.lnName));
                command.Parameters.Add(new NpgsqlParameter<string>(aniName, final.aniName));
                command.Parameters.Add(new NpgsqlParameter<string>(addName, final.addName));
                command.Parameters.Add(new NpgsqlParameter<string>(blname, final.blName));
                command.Parameters.Add(new NpgsqlParameter<string>(ctName, final.ctName));
                command.Parameters.Add(new NpgsqlParameter<string>(psName, final.psName));
                command.Parameters.Add(new NpgsqlParameter<string>(tpName, final.tpName));
                command.Parameters.Add(new NpgsqlParameter<string>(prName, final.prName));
                command.Parameters.Add(new NpgsqlParameter<string>(dtName, final.dtName));


            }
            command.CommandText = sb.ToString();
            await command.ExecuteNonQueryAsync();

            conn.Close();
            Console.WriteLine("CONNECTION CLOSED");
        }



    }
}
