using RestSharp.Serialization.Json;
using RestSharp.Serializers;

namespace Git.hub.util
{
    class ReplacingJsonSerializer : ISerializer
    {
        private JsonSerializer serializer = new();
        private string _what;
        private string _with;

        public ReplacingJsonSerializer(string what, string with)
        {
            this._what = what;
            this._with = with;
        }

        public string Serialize(object obj)
        {
            return serializer.Serialize(obj).Replace(_what, _with);
        }

        public string ContentType
        {
            get => serializer.ContentType;
            set => serializer.ContentType = value;
        }

        public string DateFormat
        {
            get => serializer.DateFormat;
            set => serializer.DateFormat = value;
        }

        public string RootElement
        {
            get => serializer.RootElement;
            set => serializer.RootElement = value;
        }
    }
}
