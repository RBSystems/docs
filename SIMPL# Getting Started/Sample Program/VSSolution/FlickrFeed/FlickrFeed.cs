using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronXml;
using Crestron.SimplSharp.Net.Http;
using Crestron.SimplSharp.CrestronXmlLinq;

namespace Crestron.SIMPLSharpSamples
{
    public class FlickrFeed
    {
        // Private member variables
        private string[] photoArray = new string[1];
        private CTimer timer;
        private string userID = "";
        private int count = 0;
        private int index = 0;
        private long timerInterval = 1200000; // default time of 20 minutes.

        // Public delegates
        public errorHandler OnError { get; set; } 
        public NewPhoto OnNewURL { get; set; } 
        public delegate void errorHandler(SimplSharpString errMsg);
        public delegate void NewPhoto(SimplSharpString URL);

        // Public Properties
        public string UserID
        {
            set { userID = value; }
        }

        public ushort SlideshowInterval
        {
            set { timerInterval = value * 60 * 1000; }
        }

        // Public Methods

        /// <summary>
        /// Load starts it all. It connects to the webserver, and gets the XML Feed.
        /// then it parses the XML and sets up the timer.
        /// </summary>
        public void Load()
        {
            HttpClient httpClient = new HttpClient();
            HttpClientRequest httpRequest = new HttpClientRequest();
            HttpClientResponse httpResponse;

            // If no UserID is filled in, and the OnError delegate has been subscribed to, the delegate is called.
            if (userID == "" && OnError != null)
                OnError(new SimplSharpString("No UserID Specified"));

            try
            {
                //Get the XML Feed from Flickr
                httpClient.KeepAlive = false;
                httpRequest.Url.Parse(String.Format("http://api.flickr.com/services/feeds/photos_public.gne?id={0}", userID));
                httpResponse = httpClient.Dispatch(httpRequest);
                
                //Load and Parse the XML response
                XDocument feedXML = XDocument.Parse(httpResponse.ContentString);
                String xmlns = "{http://www.w3.org/2005/Atom}"; //Atom namespace

                // using XML.Linq you can quickly make a selection of the entries in the XML document
                var posts = from item in feedXML.Descendants(xmlns + "entry")
                            from link in item.Descendants(xmlns + "link")
                            where link.Attribute("rel").Value == "enclosure"
                            select new
                            {
                                Url = link.Attribute("href").Value
                            };
                count = posts.Count();

                // resize the array to be the correct size
                Array.Resize(ref photoArray, count);

                // Fill the array with the JPG URLs
                int i = 0;
                foreach (var xEl in posts)
                    photoArray[i++] = xEl.Url;

                // Once this is all done, set up a timer to trigger at the interval specified.
                if (timer == null)
                    timer = new CTimer(TimerExpired, this, 1, timerInterval);
            }
            catch (Exception e)
            {
                // If an error is thrown, and the OnError delegate has been subscribed to, the delegate is called.
                if (OnError != null)
                    OnError(new SimplSharpString(e.ToString() + "\n\r" + e.StackTrace));
            }
        }

        /// <summary>
        /// TimerExpired calls the delegate to send out the new URL
        /// </summary>
        /// <param name="obj"></param>
        private void TimerExpired(object obj)
        {
            if (OnNewURL != null)
                OnNewURL(new SimplSharpString(photoArray[index]));
            if (index == count)
                index = 0;
            else
                index++;
        }
    }
}