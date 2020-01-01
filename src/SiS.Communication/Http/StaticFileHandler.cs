using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;

namespace SiS.Communication.Http
{
    /// <summary>
    /// Represents a http handler for static file request.
    /// </summary>
    public class StaticFileHandler : HttpHandler
    {
        #region Mime
        private const string MIMEString = @"0,application/octet-stream
                            323,text/h323
                            acx,application/internet-property-stream
                            ai,application/postscript
                            aif,audio/x-aiff
                            aifc,audio/x-aiff
                            aiff,audio/x-aiff
                            asf,video/x-ms-asf
                            asr,video/x-ms-asf
                            asx,video/x-ms-asf
                            au,audio/basic
                            avi,video/x-msvideo
                            axs,application/olescript
                            bas,text/plain
                            bcpio,application/x-bcpio
                            bin,application/octet-stream
                            bmp,image/bmp
                            c,text/plain
                            cat,application/vnd.ms-pkiseccat
                            cdf,application/x-cdf
                            cer,application/x-x509-ca-cert
                            class,application/octet-stream
                            clp,application/x-msclip
                            cmx,image/x-cmx
                            cod,image/cis-cod
                            cpio,application/x-cpio
                            crd,application/x-mscardfile
                            crl,application/pkix-crl
                            crt,application/x-x509-ca-cert
                            csh,application/x-csh
                            css,text/css
                            dcr,application/x-director
                            der,application/x-x509-ca-cert
                            dir,application/x-director
                            dll,application/x-msdownload
                            dms,application/octet-stream
                            doc,application/msword
                            dot,application/msword
                            dvi,application/x-dvi
                            dxr,application/x-director
                            eps,application/postscript
                            etx,text/x-setext
                            evy,application/envoy
                            exe,application/octet-stream
                            fif,application/fractals
                            flr,x-world/x-vrml
                            gif,image/gif
                            gtar,application/x-gtar
                            gz,application/x-gzip
                            h,text/plain
                            hdf,application/x-hdf
                            hlp,application/winhlp
                            hqx,application/mac-binhex40
                            hta,application/hta
                            htc,text/x-component
                            htm,text/html
                            html,text/html
                            htt,text/webviewhtml
                            ico,image/x-icon
                            ief,image/ief
                            iii,application/x-iphone
                            ins,application/x-internet-signup
                            isp,application/x-internet-signup
                            jfif,image/pipeg
                            jpe,image/jpeg
                            jpeg,image/jpeg
                            jpg,image/jpeg
                            js,application/javascript
                            json,application/json
                            latex,application/x-latex
                            lha,application/octet-stream
                            lsf,video/x-la-asf
                            lsx,video/x-la-asf
                            lzh,application/octet-stream
                            m13,application/x-msmediaview
                            m14,application/x-msmediaview
                            m3u,audio/x-mpegurl
                            man,application/x-troff-man
                            mdb,application/x-msaccess
                            me,application/x-troff-me
                            mht,message/rfc822
                            mhtml,message/rfc822
                            mid,audio/mid
                            mny,application/x-msmoney
                            mov,video/quicktime
                            movie,video/x-sgi-movie
                            mp2,video/mpeg
                            mp3,audio/mpeg
                            mpa,video/mpeg
                            mpe,video/mpeg
                            mpeg,video/mpeg
                            mpg,video/mpeg
                            mpp,application/vnd.ms-project
                            mpv2,video/mpeg
                            ms,application/x-troff-ms
                            mvb,application/x-msmediaview
                            nws,message/rfc822
                            oda,application/oda
                            p10,application/pkcs10
                            p12,application/x-pkcs12
                            p7b,application/x-pkcs7-certificates
                            p7c,application/x-pkcs7-mime
                            p7m,application/x-pkcs7-mime
                            p7r,application/x-pkcs7-certreqresp
                            p7s,application/x-pkcs7-signature
                            pbm,image/x-portable-bitmap
                            pdf,application/pdf
                            pfx,application/x-pkcs12
                            pgm,image/x-portable-graymap
                            pko,application/ynd.ms-pkipko
                            pma,application/x-perfmon
                            pmc,application/x-perfmon
                            pml,application/x-perfmon
                            pmr,application/x-perfmon
                            pmw,application/x-perfmon
                            pnm,image/x-portable-anymap
                            png,image/png
                            ppm,image/x-portable-pixmap
                            pps,application/vnd.ms-powerpoint
                            ppt,application/vnd.ms-powerpoint
                            prf,application/pics-rules
                            ps,application/postscript
                            pub,application/x-mspublisher
                            qt,video/quicktime
                            ra,audio/x-pn-realaudio
                            ram,audio/x-pn-realaudio
                            ras,image/x-cmu-raster
                            rgb,image/x-rgb
                            rmi,audio/mid
                            roff,application/x-troff
                            rtf,application/rtf
                            rtx,text/richtext
                            scd,application/x-msschedule
                            sct,text/scriptlet
                            setpay,application/set-payment-initiation
                            setreg,application/set-registration-initiation
                            sh,application/x-sh
                            shar,application/x-shar
                            sit,application/x-stuffit
                            snd,audio/basic
                            spc,application/x-pkcs7-certificates
                            spl,application/futuresplash
                            src,application/x-wais-source
                            sst,application/vnd.ms-pkicertstore
                            stl,application/vnd.ms-pkistl
                            stm,text/html
                            svg,image/svg+xml
                            sv4cpio,application/x-sv4cpio
                            sv4crc,application/x-sv4crc
                            swf,application/x-shockwave-flash
                            t,application/x-troff
                            tar,application/x-tar
                            tcl,application/x-tcl
                            tex,application/x-tex
                            texi,application/x-texinfo
                            texinfo,application/x-texinfo
                            tgz,application/x-compressed
                            tif,image/tiff
                            tiff,image/tiff
                            tr,application/x-troff
                            trm,application/x-msterminal
                            tsv,text/tab-separated-values
                            txt,text/plain
                            uls,text/iuls
                            ustar,application/x-ustar
                            vcf,text/x-vcard
                            vrml,x-world/x-vrml
                            wav,audio/x-wav
                            wcm,application/vnd.ms-works
                            wdb,application/vnd.ms-works
                            wks,application/vnd.ms-works
                            wmf,application/x-msmetafile
                            wps,application/vnd.ms-works
                            wri,application/x-mswrite
                            wrl,x-world/x-vrml
                            wrz,x-world/x-vrml
                            xaf,x-world/x-vrml
                            xbm,image/x-xbitmap
                            xla,application/vnd.ms-excel
                            xlc,application/vnd.ms-excel
                            xlm,application/vnd.ms-excel
                            xls,application/vnd.ms-excel
                            xlt,application/vnd.ms-excel
                            xlw,application/vnd.ms-excel
                            xml,application/xml
                            xof,x-world/x-vrml
                            xpm,image/x-xpixmap
                            xwd,image/x-xwindowdump
                            z,application/x-compress
                            zip,application/zip";
        #endregion

        #region Constructor
        /// <summary>
        /// Create an instance of StaticFileHandler with specific root directory of the static files.
        /// </summary>
        /// <param name="rootDir">The root directory of the static files.</param>
        public StaticFileHandler(string rootDir)
        {
            if (!Directory.Exists(rootDir))
            {
                throw new Exception($"The directory {rootDir} is not exist.");
            }
            RootDir = rootDir;
            ConstructorInit();
        }

        private void ConstructorInit()
        {
            MimeDict = new Dictionary<string, string>();
            DefaultFiles = new HashSet<string>();
            StringReader reader = new StringReader(MIMEString);
            while (true)
            {
                string strLine = reader.ReadLine();
                if (strLine == null)
                {
                    break;
                }
                strLine = strLine.Trim();
                if (strLine == "")
                {
                    break;
                }
                string[] fields = strLine.Split(',');
                MimeDict.Add(fields[0].Trim(), fields[1].Trim());
            }
        }

        /// <summary>
        /// Create an instance of StaticFileHandler with default root dir.
        /// </summary>
        public StaticFileHandler() 
        {
            RootDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
            if (!Directory.Exists(RootDir))
            {
                Directory.CreateDirectory(RootDir);
            }
            ConstructorInit();
        }

        #endregion

        #region Privte members
        private readonly HashSet<string> CompressFileExt = new HashSet<string>() { "js", "json", "htm", "html", "jsp", "asp", "xml", "css", "vue", "php", "ts" };
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the max length of the cache to save the static file in memory. 
        /// If the file size exceeds the value, a file stream will be opened for reading while sending.
        /// The default value is 4MB.
        /// </summary>
        public int MaxCacheLength { get; set; } = 4 * 1024 * 1024;

        /// <summary>
        /// Gets or sets the max size of the file that will be compressed.If the file size exceeds the value, it will not be compressed.
        /// The default value is 10MB.
        /// </summary>
        public int MaxCompressLength { get; set; } = 10 * 1024 * 1024;

        /// <summary>
        /// Gets the dictionary of mime. The user can add custom mime type to the dictionary.
        /// </summary>
        public Dictionary<string, string> MimeDict { get; private set; }

        /// <summary>
        /// Gets the collection of default files. The user can add default file to the dictionary.
        /// </summary>
        public HashSet<string> DefaultFiles { get; private set; }

        /// <summary>
        /// Gets or sets the root directory of the static files.
        /// </summary>
        public string RootDir { get; set; }

        /// <summary>
        /// Gets or sets whether enable gzip compress.
        /// </summary>
        public bool EnableGZIP { get; set; } = true;
        #endregion

        #region Public functions

        /// <summary>
        /// The function used to process http request.
        /// </summary>
        /// <param name="context">The context of the http request.</param>
        public override void Process(HttpContext context)
        {
            HttpRequestMessage reqMsg = context.Request;
            if (reqMsg.Method != HttpMethod.Get)
            {
                return;
            }
            string url = reqMsg.RequestUri.ToString();
            int paramPos = url.IndexOf("?");
            if (paramPos >= 0)
            {
                url = url.Substring(0, paramPos);
            }

            string sFilePath = HttpUtility.UrlDecode(url);
            // sFilePath = sFilePath.Replace("/", "\\");
            bool isRootRequest = sFilePath == "/";
            string filePath = null;
            if (isRootRequest)
            {
                if (DefaultFiles.Count == 0)
                {
                    return;
                }
                bool isMatch = false;
                foreach (string fileName in DefaultFiles)
                {
                    filePath = Path.Combine(RootDir, fileName);
                    if (File.Exists(filePath))
                    {
                        isMatch = true;
                        break;
                    }
                }

                if (!isMatch)
                {
                    return;
                }
            }
            else
            {
                filePath = Path.Combine(RootDir, sFilePath.TrimStart('/'));
                if (!File.Exists(filePath))
                {
                    return;
                }
            }

            string ext = Path.GetExtension(filePath);
            DateTimeOffset? reqModifyDateTime = null;
            if (context.Request.Headers.TryGetValues("If-Modified-Since", out IEnumerable<string> vals))
            {
                if (DateTimeOffset.TryParse(vals.First(), out DateTimeOffset outTime))
                {
                    reqModifyDateTime = outTime;
                }
            }
            try
            {
                FileInfo fi = new FileInfo(filePath);
                HttpResponseMessage repMsg = ResponseMsgHelper.CreateSimpleRepMsg();
                HttpContent content = null;
                DateTimeOffset fileTime = new DateTimeOffset(fi.LastWriteTimeUtc);

                bool isFileTimeSame = false;
                if (reqModifyDateTime != null)
                {
                    isFileTimeSame = fileTime.Year == reqModifyDateTime.Value.Year
                                  && fileTime.Month == reqModifyDateTime.Value.Month
                                  && fileTime.Day == reqModifyDateTime.Value.Day
                                  && fileTime.Hour == reqModifyDateTime.Value.Hour
                                  && fileTime.Minute == reqModifyDateTime.Value.Minute
                                  && fileTime.Second == reqModifyDateTime.Value.Second;
                }

                if (!isFileTimeSame)
                {
                    string fileName = Path.GetFileName(filePath);
                    string contentType = MimeDict.First().Value;
                    if (!string.IsNullOrEmpty(ext))
                    {
                        ext = ext.TrimStart('.').ToLower();
                        if (MimeDict.TryGetValue(ext, out string temp))
                        {
                            contentType = temp;
                        }
                    }

                    bool acceptGzip = EnableGZIP && reqMsg.Headers.AcceptEncoding.Any(p => p.Value.ToLower() == "gzip");
                    if (acceptGzip)
                    {
                        if (fi.Length <= 2 * 1024 || !CompressFileExt.Contains(ext) || fi.Length > MaxCompressLength)
                        {
                            acceptGzip = false;
                        }
                    }

                    if (acceptGzip || fi.Length <= MaxCacheLength)
                    {
                        FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        byte[] data = null;
                        if (acceptGzip)
                        {
                            data = DataZip.CompressToGZIPBytes(fs);
                        }
                        else
                        {
                            data = new byte[fs.Length];
                            fs.Read(data, 0, data.Length);
                        }

                        fs.Close();
                        content = new ByteArrayContent(data);
                        content.Headers.ContentLength = data.Length;

                    }
                    else
                    {
                        FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        StreamContent streamContent = new StreamContent(fs);
                        content = streamContent;
                        content.Headers.ContentLength = fi.Length;
                    }

                    MediaTypeHeaderValue headerValue = new MediaTypeHeaderValue(contentType);
                    content.Headers.ContentType = headerValue;

                    content.Headers.Add("Last-Modified", fileTime.ToString("R"));
                    if (acceptGzip)
                    {
                        content.Headers.ContentEncoding.Add("gzip");
                    }
                }
                else
                {
                    repMsg.StatusCode = System.Net.HttpStatusCode.NotModified;
                }

                repMsg.Content = content;
                context.Response = repMsg;
            }
            catch (Exception ex)
            {
                context.Response = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
                //System.Diagnostics.Trace.WriteLine(ex.ToString());
                Console.WriteLine(ex.Message);
            }
        }
        #endregion
    }
}
