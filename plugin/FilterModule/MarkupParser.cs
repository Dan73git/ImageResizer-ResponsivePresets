﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Configuration;
using HtmlAgilityPack;
using System.Xml;

namespace ResponsivePresets.FilterModule
{
    public class MarkupParser
    {
        private System.Text.Encoding TextEncoding;

        public MarkupParser(System.Text.Encoding encoding)
        {
            this.TextEncoding = encoding;
        }

        public string TransformImgToPicture(string content)
        {
            try
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(content);

                // - activating"do no harm"-mode
                doc.OptionFixNestedTags = false;
                doc.OptionAutoCloseOnEnd = false;
                doc.OptionCheckSyntax = false;
                foreach (HtmlNode img in doc.DocumentNode.SelectNodes("//img[@src]"))
                {
                    HtmlAttribute src = img.Attributes["src"];
                    if (src != null)
                    {
                        if (src.Value.ToLowerInvariant().IndexOf("preset=.") != -1)
                        {
                            HtmlNode fallbackImg = img.Clone();

                            // - make this "img" a "picture"
                            img.Name = "picture";

                            // - get presets from config
                            var configSection = WebConfigurationManager.GetSection("resizer") as ImageResizer.ResizerSection;
                            XmlElement conf = configSection.getCopyOfNode("responsivepresets").ToXmlElement();

                            // - filter configuration
                            bool respectPixelDensity = (conf.Attributes["respectPixelDensity"].Value == "true");

                            // - add "source" tag for each preset item
                            if (conf.ChildNodes != null && conf.HasChildNodes)
                            {
                                foreach (XmlNode c in conf.ChildNodes)
                                {
                                    string name = c.Attributes["prefix"].Value;
                                    if (c.Name.Equals("preset", StringComparison.OrdinalIgnoreCase))
                                    {
                                        HtmlNode source = doc.CreateElement("source");
                                        if (c.Attributes["media"]!=null)
                                            source.Attributes.Add("media", c.Attributes["media"].Value);

                                        // - generate x1, x2, x3, x4 (for now ...) if requested to do so
                                        string srcsetBase = img.Attributes["src"].Value.Replace("preset=.", "preset=" + name + ".");
                                        StringBuilder srcset = new StringBuilder();
                                        if (respectPixelDensity)
                                        {
                                            for (int i = 1; i <= 4; i++)
                                            {
                                                srcset.Append(srcsetBase + "&zoom=" + i + " " + i + "x, ");
                                            }
                                        }
                                        source.Attributes.Add("srcset", srcset.ToString().Trim().TrimEnd(','));

                                        // - add this "source" tag  as a new "img" child
                                        img.ChildNodes.Add(source);
                                    }
                                }
                            }

                            // - remove "src" from "picture" (leftover from this beeing a "img" tag)
                            img.Attributes.Remove("src");

                            // - append fallback image
                            HtmlNode fallbackContainer = doc.CreateElement("noscript");
                            fallbackImg.Attributes.Add("class", "foo, bar");
                            fallbackContainer.AppendChild(fallbackImg);
                            img.AppendChild(fallbackContainer);
                        }
                    }
                }

                return doc.DocumentNode.OuterHtml;
            }
            catch(Exception ex)
            {
                // - better that nothing(tm) ...
                //return content;
                return ex.ToString();
            }
        }

        public string ReplaceString(string str, string oldValue, string newValue, StringComparison comparison)
        {
            StringBuilder sb = new StringBuilder();

            int previousIndex = 0;
            int index = str.IndexOf(oldValue, comparison);
            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, comparison);
            }
            sb.Append(str.Substring(previousIndex));

            return sb.ToString();
        }
    }
}

