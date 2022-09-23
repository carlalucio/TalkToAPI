using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace TalkToAPI.V1.Models.DTO
{
    public class MensagemDTO : BaseDTO
    {
 
        public int Id { get; set; }
        public ApplicationUser De { get; set; }
        public string DeId { get; set; }
        public ApplicationUser Para { get; set; }       
        public string ParaId { get; set; }  
        public string Texto { get; set; }
        public bool Excluido { get; set; }
        public DateTime Criado { get; set; }
        public DateTime? Atualizado { get; set; }

    }
}
