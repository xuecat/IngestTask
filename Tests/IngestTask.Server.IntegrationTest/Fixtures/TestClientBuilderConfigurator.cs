namespace IngestTask.Server.IntegrationTest.Fixtures
{
    using Microsoft.Extensions.Configuration;
    using Orleans;
    using Orleans.Hosting;
    using Orleans.TestingHost;
    using IngestTask.Abstraction.Constants;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Linq;

    public class TestClientBuilderConfigurator : IClientBuilderConfigurator
    {
        private static string CreateConfigURI(string str)
        {
            if (str.IndexOf("http:") >= 0 || str.IndexOf("https:") >= 0)
            {
                return str;
            }
            else
                return "http://" + str;
        }

        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            
            clientBuilder.AddSimpleMessageStreamProvider(StreamProviderName.Default);
            clientBuilder.ConfigureAppConfiguration((config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                
                var dic = new Dictionary<string, string>();

                string fileName = "publicsetting.xml";
                string path = string.Empty;
                if ((Environment.OSVersion.Platform == PlatformID.Unix) || (Environment.OSVersion.Platform == PlatformID.MacOSX))
                {
                    //str = string.Format(@"{0}/{1}", System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, fileName);
                    path = '/' + fileName;
                }
                else
                {
                    path = AppDomain.CurrentDomain.BaseDirectory.Replace("\\", "/") + "/" + fileName;
                }

                if (File.Exists(path))
                {
                    XDocument xd = new XDocument();
                    xd = XDocument.Load(path);
                    XElement ps = xd.Element("PublicSetting");
                    XElement sys = ps.Element("System");

                    string vip = sys.Element("Sys_VIP").Value;
                    dic.Add("VIP", vip);
                    dic.Add("IngestDBSvr", CreateConfigURI(sys.Element("IngestDBSvr").Value));
                    dic.Add("IngestDEVCTL", CreateConfigURI(sys.Element("IngestDEVCTL").Value));
                    dic.Add("CMWindows", CreateConfigURI(sys.Element("CMserver_windows").Value));
                    dic.Add("CMServer", CreateConfigURI(sys.Element("CMServer").Value));
                    config.AddInMemoryCollection(dic);
                }
            });
        }
            
    }
}
