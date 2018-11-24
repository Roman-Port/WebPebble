using System;
using System.Collections.Generic;
using System.Text;

namespace WebPebble.Entities
{
    public class WebPebbleUserData
    {
        public int _id { get; set; } //Used in the database

        public string rpwsId { get; set; } //Used to find this object.

        public string theme { get; set; }

        public void Update()
        {
            //Update in database
            Program.database.GetCollection<WebPebbleUserData>("users").Update(this);
        }
    }
}
