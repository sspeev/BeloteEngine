﻿using BeloteEngine.Models;

namespace BeloteEngine.Data.Entities.Models
{
    public class Team
    {
        public Team()
        {
            players = new Player[2];
        }
        public Player[] players { get; set; }

        public int Points { get; set; }


    }
}