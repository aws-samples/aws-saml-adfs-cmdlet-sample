/*
 * Copyright 2010-2013 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 *
 *  http://aws.amazon.com/apache2.0
 *
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;

using System.Net;
using System.Net.Http;

using System.Xml;
using System.Text.RegularExpressions;

using Amazon.SecurityToken;
using Amazon.Runtime;
using Amazon.SecurityToken.Model;

namespace AWSSAML
{
    class AWSSAMLUtils
    {
        public string GetSamlAssertion(string identityProvider)
        {

            string samlAssertion = "";
            HttpWebResponse response = getResult(identityProvider);
            string responseStreamData;


            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                responseStreamData = reader.ReadToEnd();
            }

            Regex reg = new Regex("SAMLResponse\\W+value\\=\\\"([^\\\"]+)\\\"");
            MatchCollection matches = reg.Matches(responseStreamData);
            string last = null;
            foreach (Match m in matches)
            {
                last = m.Groups[1].Value;
                samlAssertion = last;
            }

            return samlAssertion;
        }

        public string[] GetAwsSamlRoles(string samlAssertion)
        {
            string[] awsSamlRoles = null;
            XmlDocument doc = new XmlDocument();
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            byte[] decoded = Convert.FromBase64String(samlAssertion);
            string deflated = Encoding.UTF8.GetString(decoded);

            doc.LoadXml(deflated);       
            using (XmlTextWriter tw = new XmlTextWriter(sw) { Formatting = Formatting.Indented })
            {
                doc.WriteTo(tw);
            }

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("response", "urn:oasis:names:tc:SAML:2.0:assertion");
            string xPathString = "//response:Attribute[@Name='https://aws.amazon.com/SAML/Attributes/Role']";
            XmlNodeList roleAttributeNodes = doc.DocumentElement.SelectNodes(xPathString, nsmgr);

            if (roleAttributeNodes != null && roleAttributeNodes.Count > 0)
            {
                XmlNodeList roleNodes = roleAttributeNodes[0].ChildNodes;

                awsSamlRoles = new string[roleNodes.Count];

                for (int i = 0; i < roleNodes.Count; i++)
                {
                    XmlNode roleNode = roleNodes[i];
                    if (roleNode.InnerText.Length > 0)
                    {
                        string[] chunks = roleNode.InnerText.Split(',');
                        string newAwsSamlRole = chunks[0] + ',' + chunks[1];
                        awsSamlRoles[i] = newAwsSamlRole;
                    }
                }
            }

            return awsSamlRoles;
        }

        public SessionAWSCredentials GetSamlRoleCredentails(string samlAssertion, string awsRole)
        {
            string[] role = awsRole.Split(',');

            AssumeRoleWithSAMLRequest samlRequest = new AssumeRoleWithSAMLRequest();
            samlRequest.SAMLAssertion = samlAssertion;
            samlRequest.RoleArn = role[1];
            samlRequest.PrincipalArn = role[0];
            samlRequest.DurationSeconds = 3600;

            AmazonSecurityTokenServiceClient sts;
            AssumeRoleWithSAMLResponse samlResponse;
            try { 
                sts = new AmazonSecurityTokenServiceClient();
                samlResponse = sts.AssumeRoleWithSAML(samlRequest);
            }
            catch
            {
                sts = new AmazonSecurityTokenServiceClient("a", "b", "c");
                samlResponse = sts.AssumeRoleWithSAML(samlRequest);
            }

            SessionAWSCredentials sessionCredentials = new SessionAWSCredentials(
                samlResponse.Credentials.AccessKeyId,
                samlResponse.Credentials.SecretAccessKey,
                samlResponse.Credentials.SessionToken);

            return sessionCredentials;
        }

        private HttpWebResponse getResult(string url)
        {
            Uri uri = new Uri(url);

            CredentialCache credCache = new CredentialCache();
            credCache.Add(uri, "NTLM", CredentialCache.DefaultNetworkCredentials);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";
            request.KeepAlive = true;
            request.Credentials = credCache;
            request.PreAuthenticate = true;
            request.AllowAutoRedirect = true;
            request.CookieContainer = new System.Net.CookieContainer();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            return response;    
        }
    }
}
