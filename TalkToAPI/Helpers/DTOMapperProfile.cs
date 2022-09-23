using AutoMapper;
using System.Collections.Generic;
using TalkToAPI.V1.Models;
using TalkToAPI.V1.Models.DTO;

namespace TalkToAPI.Helpers
{   /*AutoMapper é a biblioteca que nos ajuda a copiar um objeto de um tipo, para outro. Ele ignora atributos que não tenham equivalência
     ex = Palavra > PalavraDTO 
     Aqui no Profile vamos determinar quais objetos ele irá mapear
    Podse usar o .ForMember() para determinar a cópia de  atributos com nomes diferentes*/

    public class DTOMapperProfile: Profile
    {
        public DTOMapperProfile()
        {
            //primeiro recebe o objeto origem, dps o que vai receber os valores
            //usar a biblioteca para ele associar a propriedade FullName do AplicationUser ao Nome do UsuarioDTO
            //o FormMember pega a propriedade do destino e indica qual propriedade ela vai receber
            CreateMap<ApplicationUser, UsuarioDTOSemHyperlink>()
                 .ForMember(dest => dest.Nome, orig => orig.MapFrom(src => src.FullName));


            //cria mapeamento de AppUser para usuario sem links
            CreateMap<ApplicationUser, UsuarioDTO>()
                .ForMember(dest => dest.Nome, orig => orig.MapFrom(src => src.FullName)); ;

            //cria o mapaeamento de Mensagem para MensagemDTO
            CreateMap<Mensagem, MensagemDTO>();


            
            
         
        }
    }
}
