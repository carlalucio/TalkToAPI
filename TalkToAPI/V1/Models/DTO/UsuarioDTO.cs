using System.ComponentModel.DataAnnotations;

namespace TalkToAPI.V1.Models.DTO
{
    //esse usuario não vai para o banco de dados. Vamos usá-lo para receber o formulário para cadastro e login de usuario
    public class UsuarioDTO : BaseDTO
    {
        public string Id { get; set; }
        [Required]
        public string Nome { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Senha { get; set; }
        [Required]
        [Compare("Senha")]
        public string ConfirmacaoSenha { get; set; }
        public string Slogan { get; set; }
    }
}
