using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using MSCLoader;

namespace MSCStill
{
	public class SaveUtil
	{
		public static void SerializeWriteFile<T>(T value, string path)
		{
			try
			{
				var xmlserializer = new XmlSerializer(typeof(T));
				var stream = new StreamWriter(path);
				var writer = XmlWriter.Create(stream);
				xmlserializer.Serialize(writer, value);
				writer.Close();
			}
			catch (Exception ex)
			{
				ModConsole.Error(ex.ToString());
			}
		}

		public static T DeserializeReadFile<T>(string path) where T:new()
		{
			try
			{
				var xmlserializer = new XmlSerializer(typeof(T));
				var stream = new StreamReader(path);
				var writer = XmlReader.Create(stream);
				return (T)xmlserializer.Deserialize(writer);
			}
			catch (Exception ex)
			{
				ModConsole.Error(ex.ToString());
			}
			return new T();
		}
	}
}
