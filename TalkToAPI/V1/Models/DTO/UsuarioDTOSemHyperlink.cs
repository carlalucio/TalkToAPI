﻿using System.ComponentModel.DataAnnotations;

namespace TalkToAPI.V1.Models.DTO
{
    public class UsuarioDTOSemHyperlink
    {
        public string Id { get; set; }
       
        public string Nome { get; set; }
   
        public string Email { get; set; }
     
        public string Slogan { get; set; }
    }
}
