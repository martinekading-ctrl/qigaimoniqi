using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Reborn1985.M4
{
    // One explicit manual slot. Temp + Flush + atomic replace, retaining the last valid file.
    public sealed class M4SaveStore
    {
        [Serializable] private sealed class Envelope { public int version=1; public string payload, sha256; }
        public string Path { get; }
        public M4SaveStore(string directory) { Path=System.IO.Path.Combine(directory,"save.json"); }
        private static string Hash(string payload)
        { using (var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-",""); }
        public void Save(M4SaveData data, Func<M4SaveData,bool> validate)
        {
            if (!validate(data)) throw new InvalidDataException("Current state is invalid; existing save retained.");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            string payload=JsonUtility.ToJson(data);
            byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Envelope {payload=payload,sha256=Hash(payload)},true));
            string temp=Path+".tmp";
            using (var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
            { stream.Write(bytes,0,bytes.Length); stream.Flush(true); }
            if (File.Exists(Path))
            {
                bool validOld=TryRead(Path,validate,out _,out _);
                File.Replace(temp,Path,validOld?Path+".bak":null,true);
            }
            else File.Move(temp,Path);
        }
        public bool Load(Func<M4SaveData,bool> validate, out M4SaveData data, out bool backup, out string error)
        {
            backup=false;
            if (TryRead(Path,validate,out data,out error)) return true;
            string primaryError=error;
            if (TryRead(Path+".bak",validate,out data,out error)) { backup=true; return true; }
            error=primaryError+"; backup: "+error; return false;
        }
        private static bool TryRead(string path, Func<M4SaveData,bool> validate, out M4SaveData data, out string error)
        {
            data=null; error="";
            try
            {
                if (!File.Exists(path)) throw new FileNotFoundException("No saved game");
                if (new FileInfo(path).Length>1000000) throw new InvalidDataException("Save exceeds size limit");
                Envelope envelope=JsonUtility.FromJson<Envelope>(File.ReadAllText(path,Encoding.UTF8));
                if (envelope==null || envelope.version!=1 || string.IsNullOrEmpty(envelope.payload) || !string.Equals(envelope.sha256,Hash(envelope.payload),StringComparison.Ordinal))
                    throw new InvalidDataException("Unsupported or damaged save envelope");
                var candidate=JsonUtility.FromJson<M4SaveData>(envelope.payload);
                if (candidate==null || !validate(candidate)) throw new InvalidDataException("Saved state failed validation");
                data=candidate; return true;
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is FormatException)
            { error=ex.Message; return false; }
        }
    }
}
