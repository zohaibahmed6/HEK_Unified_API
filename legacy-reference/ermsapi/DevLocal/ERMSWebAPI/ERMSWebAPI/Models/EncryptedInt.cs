using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERMSWebAPI.Models
{
    [JsonConverter(typeof(EncryptedIntJsonConverter))]
    public class EncryptedInt
    {

        private readonly string _value;

        public string Value
        {
            get { return this._value; }
        }


        public EncryptedInt()
        {

        }


        public EncryptedInt(string value)
        {
            this._value = value;
        }

        public static implicit operator string(EncryptedInt d)
        {
            //check added by m.tayyab because it was crashing on null id
            if (d == null)
            {
                return string.Empty;
            }
            else
            {
                return d.Value;
            }
        }

        public static implicit operator Int64(EncryptedInt d)
        {
            if (d == null)
            {
                return 0;
            }
            Int64 ovlaue = 0;
            if (Int64.TryParse(d._value, out ovlaue))
                return ovlaue;

         //   Int64.TryParse(EncryptionManager.GetDecryptString(d._value.ToString()), out ovlaue);
            return ovlaue;

        }

        public static implicit operator int(EncryptedInt d)
        {
            if (d == null)
            {
                return 0;
            }
            int ovlaue = 0;
            if (int.TryParse(d._value, out ovlaue))
                return ovlaue;

          // int.TryParse(EncryptionManager.GetDecryptString(d._value.ToString()), out ovlaue);
            return ovlaue;


        }

        public static implicit operator EncryptedInt(string d)
        {
            return new EncryptedInt(d);
        }

        public static implicit operator EncryptedInt(int d)
        {

            return new EncryptedInt(EncryptionManager.GetEncryptedString(d));
        }

        public static implicit operator EncryptedInt(Int64 d)
        {

            return new EncryptedInt(EncryptionManager.GetEncryptedString(d));
        }

        public override string ToString()
        {
            return this.Value;
        }


        class EncryptedIntJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(EncryptedInt);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var id = (EncryptedInt)value;
                serializer.Serialize(writer, id.Value);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var vlaue = serializer.Deserialize<string>(reader);
                return new EncryptedInt(vlaue);
            }
        }

    }
}
