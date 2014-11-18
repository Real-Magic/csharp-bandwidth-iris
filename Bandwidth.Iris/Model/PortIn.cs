﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using Bandwidth.Iris.Model;

namespace Bandwidth.Iris.Model
{
    [XmlType("LnpOrder")]
    public class PortIn: BaseModel
    {
        private const string PortInPath = "portins";
        private const string LoasPath = "loas";

        public async static Task<PortIn> Create(Client client, PortIn order)
        {
            var item = (PortIn)(await client.MakePostRequest<LnpOrderResponse>(client.ConcatAccountPath(PortInPath), order));
            item.Client = client;
            return item;
        }

#if !PCL
        public static Task<PortIn> Create(PortIn order)
        {
            return Create(Client.GetInstance(), order);
        }
#endif

        public async Task<string> CreateFile(Stream stream, string mediaType)
        {
            using (var message = await Client.SendData(Client.ConcatAccountPath(string.Format("{0}/{1}/{2}", PortInPath, Id, LoasPath)), stream, mediaType))
            {
                return
                    XDocument.Parse(await message.Content.ReadAsStringAsync())
                        .Descendants("filename")
                        .FirstOrDefault()
                        .Value;
            }
        }

        public async Task<string> CreateFile(byte[] buffer, string mediaType)
        {
            using (var message = await Client.SendData(Client.ConcatAccountPath(string.Format("{0}/{1}/{2}", PortInPath, Id, LoasPath)), buffer, mediaType))
            {
                return
                    XDocument.Parse(await message.Content.ReadAsStringAsync())
                        .Descendants("filename")
                        .FirstOrDefault()
                        .Value;
            }
        }

        public Task UpdateFile(string fileName, Stream stream, string mediaType)
        {
            return Client.SendData(Client.ConcatAccountPath(string.Format("{0}/{1}/{3}/{2}", PortInPath, Id, fileName, LoasPath)), stream, mediaType, "PUT", true);
        }

        public Task UpdateFile(string fileName, byte[] buffer, string mediaType)
        {
            return Client.SendData(Client.ConcatAccountPath(string.Format("{0}/{1}/{3}/{2}", PortInPath, Id, fileName, LoasPath)), buffer, mediaType, "PUT", true);
        }

        public Task<FileMetadata> GetFileMetadata(string fileName)
        {
            return
                Client.MakeGetRequest<FileMetadata>(
                    Client.ConcatAccountPath(string.Format("{0}/{1}/{3}/{2}/metadata", PortInPath, Id, fileName, LoasPath)));
        }

        public Task DeleteFile(string fileName)
        {
            return
                Client.MakeDeleteRequest(
                    Client.ConcatAccountPath(string.Format("{0}/{1}/{3}/{2}", PortInPath, Id, fileName, LoasPath)));
        }

        public async Task<FileData[]> GetFiles(bool metadata = false)
        {
            var r = (await Client.MakeGetRequest<FileListResponse>(
                Client.ConcatAccountPath(string.Format("{0}/{1}/{2}", PortInPath, Id, LoasPath)),
                new Dictionary<string, object> {{"metadata", metadata.ToString().ToLowerInvariant()}}));
            return (r == null)?new FileData[0]: r.FileData.ToArray();
        }

        public async Task<FileContent> GetFile(string fileName, bool asStream = false)
        {
            if (fileName == null) throw new ArgumentNullException("fileName");

            var response = await Client.MakeGetRequest(
                    Client.ConcatAccountPath(string.Format("{0}/{1}/{2}/{3}", PortInPath, Id, LoasPath, fileName)));
            var content = new FileContent(response) { MediaType = response.Content.Headers.ContentType.MediaType };
            if (asStream)
            {
                content.Stream = await response.Content.ReadAsStreamAsync();
            }
            else
            {
                content.Buffer = await response.Content.ReadAsByteArrayAsync();
            }
            return content;
        }

        public Task Update(LnpOrderSupp item)
        {
            return Client.MakePutRequest(Client.ConcatAccountPath(string.Format("{0}/{1}", PortInPath, Id)),
                item, true);
        }
        public Task Delete()
        {
            return
                Client.MakeDeleteRequest(
                    Client.ConcatAccountPath(string.Format("{0}/{1}", PortInPath, Id)));
        }
        public override string Id {
            get { return OrderId; }
            set { OrderId = value; } 
        }
        public string OrderId { get; set; }
        public string BillingTelephoneNumber { get; set; }
        public Subscriber Subscriber { get; set; }
        public string LoaAuthorizingPerson { get; set; }

        [XmlArrayItem("PhoneNumber")]
        public string[] ListOfPhoneNumbers { get; set; }
        public string SiteId { get; set; }
        public string ProcessingStatus { get; set; }
        public Status Status { get; set; }
        public bool Triggered { get; set; }
        public string BillingType { get; set; }

    }

   
    public class LnpOrderResponse : PortIn
    {
        
    }

    public class LnpOrderSupp: PortIn
    {
        public DateTime RequestedFocDate { get; set; }
        public WirelessInfo WirelessInfo { get; set; }
    }

    public class WirelessInfo
    {
        public string AccountNumber { get; set; }
        public string PinNumber { get; set; }
    }

    public class Subscriber
    {
        public string SubscriberType { get; set; }
        public string BusinessName { get; set; }
        public Address ServiceAddress { get; set; }
    }

    
    public class Status
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }

    [XmlType("FileMetaData")]
    public class FileMetadata
    {
        public string DocumentType { get; set; }
        public string DocumentName { get; set; }
    }

    [XmlType("fileListResponse")]
    public class FileListResponse
    {
        [XmlElement("fileCount")]
        public int FileCount { get; set; }

        [XmlElement("fileData")]
        public FileData[] FileData { get; set; }
    }

    public class FileData
    {
        public string FileName { get; set; }
        [XmlElement("FileMetaData")]
        public FileMetadata FileMetadata { get; set; }
    }

    public sealed class FileContent : IDisposable
    {
        private readonly IDisposable _owner;

        internal FileContent(IDisposable owner)
        {
            if (owner == null) throw new ArgumentNullException("owner");
            _owner = owner;
        }

        public string MediaType { get; set; }
        public Stream Stream { get; set; }
        public byte[] Buffer { get; set; }

        public void Dispose()
        {
            _owner.Dispose();
        }
    }
}
