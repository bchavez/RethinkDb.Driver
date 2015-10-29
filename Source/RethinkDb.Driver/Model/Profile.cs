using System;
using Newtonsoft.Json.Linq;

namespace RethinkDb.Driver.Model
{
    public class Profile
    {
        public static Profile FromJsonArray(JArray profileObj)
        {
            if( profileObj == null )
            {
                return null;
            }
            throw new Exception("fromJSONArray not implemented");
        }
    }
}