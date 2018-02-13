using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;

using Microsoft.Net.Http.Headers;

namespace Uploader.Web.Helper
{
    public static class FileStreamingHelper
    {
        private static readonly FormOptions _defaultFormOptions = new FormOptions();

        public static async Task<FormValueProvider> StreamFile(this HttpRequest request, Stream targetStream)
        {
            if (!MultipartRequestHelper.IsMultipartContentType(request.ContentType))
            {
                throw new Exception($"Expected a multipart request, but got {request.ContentType}");
            }

            // Used to accumulate all the form url encoded key value pairs in the 
            // request.
            var formAccumulator = new KeyValueAccumulator();
            string boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, request.Body, 81920);

            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                ContentDispositionHeaderValue contentDisposition;
                bool hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out contentDisposition);

                if (hasContentDispositionHeader)
                {
                    if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                    {
                        await section.Body.CopyToAsync(targetStream);
                    }
                    else if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                    {
                        // Content-Disposition: form-data; name="key"
                        //
                        // value

                        // Do not limit the key name length here because the 
                        // multipart headers length limit is already in effect.
                        string key = HeaderUtilities.RemoveQuotes(contentDisposition.Name).Value;
                        Encoding encoding = GetEncoding(section);
                        using(var streamReader = new StreamReader(
                            section.Body,
                            encoding,
                            detectEncodingFromByteOrderMarks : true,
                            bufferSize : 1024,
                            leaveOpen : true))
                        {
                            // The value length limit is enforced by MultipartBodyLengthLimit
                            string value = await streamReader.ReadToEndAsync();
                            if (string.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                            {
                                value = string.Empty;
                            }
                            formAccumulator.Append(key, value);

                            if (formAccumulator.ValueCount > _defaultFormOptions.ValueCountLimit)
                            {
                                throw new InvalidDataException($"Form key count limit {_defaultFormOptions.ValueCountLimit} exceeded.");
                            }
                        }
                    }
                }

                // Drains any remaining section body that has not been consumed and
                // reads the headers for the next section.
                section = await reader.ReadNextSectionAsync();
            }

            // Bind form data to a model
            var formValueProvider = new FormValueProvider(
                BindingSource.Form,
                new FormCollection(formAccumulator.GetResults()),
                CultureInfo.CurrentCulture);

            return formValueProvider;
        }

        private static Encoding GetEncoding(MultipartSection section)
        {
            MediaTypeHeaderValue mediaType;
            bool hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out mediaType);
            // UTF-7 is insecure and should not be honored. UTF-8 will succeed in 
            // most cases.
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }
            return mediaType.Encoding;
        }
    }
}