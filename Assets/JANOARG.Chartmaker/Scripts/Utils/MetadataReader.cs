
/*
	I wrote this thing 4 years ago
	yes, there are goto statements in c#
*/

using System;
using System.Collections.Generic;
using System.IO;

namespace JANOARG.Chartmaker.Utils
{
	public class MetadataReader {

		// ---- Classes ----

		public enum TagLibraries {
			None          = 0,
			Error         = -1,
			ID3Ver1       = 31,
			ID3Ver2Point2 = 32,
			ID3Ver2Point3 = 33,
			ID3Ver2Point4 = 34,
			VorbisComment = 100,
		}

		public class PopularimeterClass {
			public string Email    = "";
			public byte   Value    = 0;
			public byte[] Counters = new byte[0];

			public PopularimeterClass (string email, byte value, byte[] counters) {
				Email = email;
				Value = value;
				Counters = counters;
			}
			public PopularimeterClass () {
			}
		}

		public class AttachmentData
		{
			public string Name;
			public string Type;
			public byte[] Data;
		}

		// ---- Embed tags ----

		// Obsolete
	
		// Usable

		/// The audio's encoding. Returns a blank string if not declared.
		public readonly string EncodingOptions = "";
	
		/// The audio's title. Returns a blank string if not declared.
		public readonly string Title = "";
	
		/// The audio's subtitle. Returns a blank string if not declared.
		public readonly string Subtitle = "";
	
		/// The audio's artist/author. Returns a blank string if not declared.
		public readonly string Artist = "";
	
		/// The artist's URL link. Returns a blank string if not declared.
		public readonly string ArtistURL = "";
		/// The audio's performer/conductor. Returns a blank string if not declared.
		public readonly string Performer = "";
	
		/// The audio's composer. Returns a blank string if not declared.
		public readonly string Composer = "";
	
		/// The audio's type/genre as string. Returns a blank string if not declared.
		public readonly string Type = "";
	
		/// The audio album's name. Returns a blank string if not declared.
		public readonly string Album = "";
	
		/// The track position of the audio file as string. Returns a blank string if not declared.
		public readonly string Track = "";
	
		/// The audio album's artist. Returns a blank string if not declared.
		public readonly string AlbumAuthor = "";
	
		/// The file publisher. Returns a blank string if not declared.
		public readonly string Publisher = "";
	
		/// Copyright message attached to the file. Returns a blank string if not declared.
		public readonly string Copyright = "";
	
		/// The recording code attached to the file. Returns a blank string if not declared.
		public readonly string RecordingCode = "";
	
		/// Comment attached to the file. Returns a blank string if not declared.
		public readonly string Comments = "";
	
		/// Other field attached to the file. Returns a blank string if not declared.
		public readonly string Other = "";
	
		/// The track position of the audio file as integer. Returns -1 if not declared.
		public readonly int TrackInt = -1;
	
		/// Returns the recorded year of the audio if declared, else returns -1.
		public readonly int RecordedYear = -1;
	
		/// The date that the file has been encoded.
		public readonly DateTime EncodedDate = new DateTime();
	
		/// A list of attached embeds 
		public readonly List<AttachmentData> Attachments = new();
	
		/// Returns the BPM rate of the audio file if declared, else returns -1.
		public readonly float BeatsPerMinute = -1;
	
		/// Returns the audio length of the file in milliseconds.
		public readonly int LengthInMilliseconds = 0;
	
		/// The file's tag library.
		public readonly TagLibraries TagLibrary = TagLibraries.None;
		

		// ---- Reading tags ----

		/// Generates an audio metadata list using the file from the given path.
		public MetadataReader(string path, bool debugMode = false) {
			if (!File.Exists (path))
				throw new ArgumentException ("Path does not exist.");
		
			// ready to read tags
			FileStream stream = new FileStream (path, FileMode.Open);
			BinaryReader reader = new BinaryReader(stream);
			byte[] header = new byte[5];
		
			stream.Seek(0, SeekOrigin.Begin);
		
			reader.Read(header, 0, header.Length);
		
			if (header[0] == 'O' && header[1] == 'g' && header[2] == 'g' && header[3] == 'S')
			{
				// read OGG packets
				stream.Seek(0, SeekOrigin.Begin);
				List<byte> metadata = new();
				for (int i = 0; i < 2; i++) 
				{
					byte[] tag = new byte[4];
					reader.Read(tag, 0, tag.Length);
				
					byte[] version = new byte[1];
					reader.Read(version, 0, version.Length);
				
					byte[] flags = new byte[1];
					reader.Read(flags, 0, flags.Length);
				
					byte[] position = new byte[8];
					reader.Read(position, 0, position.Length);
				
					byte[] serial = new byte[4];
					reader.Read(serial, 0, serial.Length);
				
					byte[] counter = new byte[4];
					reader.Read(counter, 0, counter.Length);
				
					byte[] checksum = new byte[4];
					reader.Read(checksum, 0, checksum.Length);
				
					byte[] segsCount = new byte[1];
					reader.Read(segsCount, 0, segsCount.Length);
				
					byte[] segs = new byte[segsCount[0]];
					reader.Read(segs, 0, segs.Length);
				
					int size = 0;
				
					for (int a = 0; a < segs.Length; a++)
						size += segs[a];
				
					byte[] data = new byte[size];
					reader.Read(data, 0, data.Length);
				
					uint pos = (uint)position[3] << 24 | (uint)position[2] << 16 | (uint)position[1] << 8 | position[0];
				
					if (i == 1)
						for (int a = 0; a < size; a++)
							metadata.Add(data[a]);
				
					if (pos == 0xFFFFFFFF)
						i--;
				}
			
				byte[] mdata = metadata.ToArray();
			
				metadata.Clear();
			
				if (System.Text.Encoding.ASCII.GetString(mdata, 1, 6) == "vorbis" && mdata[0] == 3)
				{
					uint vendorLength = (uint)mdata[10] << 24 | (uint)mdata[9] << 16 | (uint)mdata[8] << 8 | mdata[7];
				
					string vendor = System.Text.Encoding.UTF8.GetString(mdata, 11, (int)vendorLength);
				
					UnityEngine.Debug.Log(vendorLength + " " + vendor);
			
					uint p = 11 + vendorLength;
					uint tagCounts = (uint)mdata[p+3] << 24 | (uint)mdata[p+2] << 16 | (uint)mdata[p+1] << 8 | mdata[p];
				
					UnityEngine.Debug.Log(tagCounts);
				
					p += 4;
				
					for (int i = 0; i < tagCounts; i++) 
					{
						uint tagLength = (uint)mdata[p+3] << 24 | (uint)mdata[p+2] << 16 | (uint)mdata[p+1] << 8 | mdata[p];
					
						string tag = System.Text.Encoding.UTF8.GetString(mdata, (int)p+4, (int)tagLength);
				
						p += 4 + tagLength;
					
						int sep = tag.IndexOf('=');
						string key = tag[..sep].ToUpper();
						string value = tag[(sep+1)..];
					
						switch (key) 
						{
							case "TITLE" : Title  = value; break;
							case "ARTIST": Artist = string.IsNullOrEmpty(Artist)   ? value : Artist + ", " + value; break;
							case "ALBUM" : Album  = value; break;
							case "GENRE" : Type   = string.IsNullOrEmpty(Type)     ? value : Type + ", " + value; break;
							case "BPM"   : float.TryParse(value, out BeatsPerMinute); break;
						}
					}
					TagLibrary = TagLibraries.VorbisComment;
				}
			}
			else try 
				{
					// read ID3v1 tags
					byte[] sample = new byte[128];
			
					stream.Seek(-128, SeekOrigin.End);
					stream.Read(sample, 0, 128);
			
					string textToGet = System.Text.Encoding.Default.GetString(sample, 0, 3);
			
					if (textToGet.CompareTo ("TAG") == 0) {
						TagLibrary = TagLibraries.ID3Ver1;
				
						Title  = System.Text.Encoding.Default.GetString(sample, 3, 30);
						Artist = System.Text.Encoding.Default.GetString(sample, 33, 30);
						Album  = System.Text.Encoding.Default.GetString(sample, 63, 30);

						try
						{
							RecordedYear = Convert.ToInt32(System.Text.Encoding.Default.GetString(sample, 93, 4));
						}
						catch (FormatException)
						{
							RecordedYear = -1;
						}
					}

					// read ID3v2 tags
					stream.Seek(0, SeekOrigin.Begin);
		
					byte[] tag = new byte[3];
					byte[] version = new byte[2];
					byte[] flag = new byte[1];
					byte[] length = new byte[4];
			
					reader.Read(tag,     0, tag.Length);
					reader.Read(version, 0, version.Length);
					reader.Read(flag,    0, flag.Length);
					reader.Read(length,  0, length.Length);
			
					ulong totalSize = (ulong)length [0] << 21 | (ulong)length [1] << 14 | (ulong)length [2] << 7 | (ulong)length [3];
		
					if (debugMode)
						UnityEngine.Debug.Log(totalSize);
		
					ulong readedSize = 0;

					if (System.Text.Encoding.Default.GetString (tag) == "ID3" && (version [0] == 0x03 || version [0] == 0x04) && version [1] == 0x00) 
					{
						if (version [0] == 0x03) TagLibrary = TagLibraries.ID3Ver2Point3;
						if (version [0] == 0x04) TagLibrary = TagLibraries.ID3Ver2Point4;

						byte[] id = new byte[4];
						byte[] size = new byte[4];
						byte[] frameFlag = new byte[2];
			
						startReading:
						reader.Read(id, 0, id.Length);
						reader.Read(size, 0, size.Length);
						reader.Read (frameFlag, 0, frameFlag.Length);
				
						ulong dataSize = (ulong)size [0] << 24 | (ulong)size [1] << 16 | (ulong)size [2] << 8 | (ulong)size [3];
			
						readedSize += dataSize + 10;
				
						byte[] body = new byte[dataSize];
			
						if (debugMode) 
							UnityEngine.Debug.Log(readedSize + " | " + dataSize + " | " + System.Text.Encoding.Default.GetString (id));
				
						reader.Read(body, 0, body.Length);
				
						if (true)
						{
							switch (System.Text.Encoding.Default.GetString (id))
							{
								// "TSSE" = Encoding Options
								case "TSSE" when body.Length > 1:
									EncodingOptions = body [0] == 0x01 
										? System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim() 
										: System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
									break;

								// "TENC" = Encoding Options
								case "TENC" when body.Length > 1:
									EncodingOptions = body [0] == 0x01 
										? System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim() 
										: System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
									break;

								// "TIT2" = Title
								case "TIT2" when body.Length > 1:
									Title = body [0] == 0x01 
										? System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim() 
										: System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
									break;

								// "TPE1" = Artist
								case "TPE1" when body.Length > 1:
									Artist = body [0] == 0x01 
										? System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim() 
										: System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
									break;

								// "TCOM" = Composer
								case "TCOM" when body.Length > 1:
									Composer = body [0] == 0x01 
										? System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim() 
										: System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
									break;

								// "TPE3" = Performer;
								case "TPE3" when body.Length > 1:
									Performer = body [0] == 0x01 
										? System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim()
										: System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
									break;

								// "TALB" = Album
								case "TALB" when body.Length > 1:
									Album = body [0] == 0x01 
										? System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim() 
										: System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
									break;

								// "TPE2" = Album Author
								case "TPE2" when body.Length > 1:
									AlbumAuthor = body [0] == 0x01 
										? System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim() 
										: System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
									break;

								// "TYER" = Recorded Year
								case "TYER" when body.Length > 1:
								{
									string TempString = "";
							
									TempString = body [0] == 0x01 
										? System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim() 
										: System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
							
									int.TryParse(TempString, out RecordedYear);
									break;
								}

								// "TDEN" = Date encoded
								case "TDEN" when body.Length > 1:
								{
									string TempString = "";
							
									TempString = body [0] == 0x01 
										? System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim() 
										: System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
							
									DateTime.TryParse(TempString, out EncodedDate);
									break;
								}

								// "TRCK" = Track
								case "TRCK" when body.Length > 1:
								{
									Track = body[0] == 0x01 
										? "#" + System.Text.Encoding.Unicode.GetString(body, 3, body.Length - 3).Trim() 
										: "#" + System.Text.Encoding.Default.GetString(body, 1, body.Length - 1).Trim();

									if (Track.IndexOf('/') >= 0)
										Track = Track.Substring(0, Track.IndexOf('/'));
						
									TrackInt = Convert.ToInt32(Track.Remove(0, Track.IndexOf('#') + 1));
									Track = "#" + TrackInt;

									break;
								}

								// "TPUB" = Publisher
								case "TPUB" when body.Length > 1:
									Publisher = body[0] == 0x01 
										? "#" + System.Text.Encoding.Unicode.GetString(body, 3, body.Length - 3).Trim() 
										: "#" + System.Text.Encoding.Default.GetString(body, 1, body.Length - 1).Trim();
									break;

								// "TBPM" = Beats per Minute
								case "TBPM" when body.Length > 1:
								{
									string TempString = "";
							
									TempString = body [0] == 0x01 
										? System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim() 
										: System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
							
									BeatsPerMinute = Convert.ToSingle(TempString);

									break;
								}

								// "TLEN" = Length
								case "TLEN" when body.Length > 1:
								{
									string TempString = "";
							
									TempString = body [0] == 0x01 
										? System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim() 
										: System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
							
									int.TryParse(TempString, out LengthInMilliseconds);

									break;
								}

								// "COMM" = Comment
								case "COMM" when body.Length > 1:
								{
									List<byte> newArray = new List<byte> (body);
						
									for (int a = 0; a<4; a++) {
										newArray = new List<byte> (body);
							
										newArray.RemoveAt (0);
										body = newArray.ToArray ();
									}

									Comments += body[0] == 0x01
										? System.Text.Encoding.Unicode.GetString(body, 3, body.Length - 3).Trim() + ", " 
										: System.Text.Encoding.Default.GetString(body, 1, body.Length - 1).Trim() + ", ";
									break;
								}

								// "TCON" = Type
								case "TCON" when body.Length > 1:
									Type = body [0] == 0x01 
										? System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim() 
										: System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
									break;

								// "TCOP" = Copyright
								case "TCOP" when body.Length > 1:
									Copyright = body [0] == 0x01 
										? System.Text.Encoding.Unicode.GetString (body, 3, body.Length - 3).Trim() 
										: System.Text.Encoding.Default.GetString (body, 1, body.Length - 1).Trim();
									break;

								// "APIC" = Attached image
								case "APIC" when body.Length > 1:
								{
									string type;
									int cur = 1;
							
									while (body[cur] != 0x00) 
										cur++;
							
									type = System.Text.Encoding.Default.GetString (body, 1, cur - 1).Trim();
									cur++; 

									byte location = body[cur]; cur++;
							
									string[] locationMap = new string[] 
									{
										"Other",
										"File icon",
										"File icon",
										"Cover (front)",
										"Cover (back)",
										"Leaflet",
										"Media",
										"Lead artist/Lead performer/Soloist",
										"Artist/Performer",
										"Conductor",
										"Band/Orchestra",
										"Composer",
										"Lyricist/Text writer",
										"Recording location",
										"During recording",
										"During performance",
										"Movie/Video screen capture",
										"🐟",
										"Illustration",
										"Band/Artist logotype",
										"Publisher/Studio logotype",
									};

									string description;
						
									int start = cur;
						
									while (body[cur] != 0x00 && (body [0] == 0x00 || body [cur-1] != 0x00))
										cur++;
						
									description = body [start] == 0x01 
										? System.Text.Encoding.Unicode.GetString (body, start, cur - start).Trim() 
										: System.Text.Encoding.Default.GetString (body, start, cur - start).Trim();
							
									cur++;

									AttachmentData att = new () 
									{
										Name = string.IsNullOrWhiteSpace(description) 
											? (location >= 0 && location < locationMap.Length ? locationMap[location] + ", " : "") + type 
											: description,
								
										Type = type,
										Data = body[cur..],
									};
									Attachments.Add(att);
									break;
								}
							}

							if (readedSize < totalSize)
								goto startReading;
						}
					}
				} 
				catch (Exception e)
				{
					UnityEngine.Debug.LogWarning("WARNING: Throwing execption while reding tags may break things! Details: " + e.Message);
					TagLibrary = TagLibraries.Error;
				}
			
			// close reading
			// doneReading:
			stream.Close();
			reader.Close();
			if (debugMode)
				UnityEngine.Debug.Log (TagLibrary.ToString());
		}

		byte[] RemoveLast (byte[] array) {
			List<byte> newArray = new List<byte> (array);
		
			newArray.RemoveAt (0);
			return newArray.ToArray ();
		}
		
	}
}
