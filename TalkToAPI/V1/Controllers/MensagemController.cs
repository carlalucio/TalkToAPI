using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using TalkToAPI.Helpers.Constantes;
using TalkToAPI.V1.Models;
using TalkToAPI.V1.Models.DTO;
using TalkToAPI.V1.Repositories.Contracts;

namespace TalkToAPI.V1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [EnableCors] //habilita a politica padrão
    public class MensagemController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IMensagemRepository _mensagemRepository;

        public MensagemController(IMapper mapper, IMensagemRepository mensagemRepository)
        {
            _mapper = mapper;   
            _mensagemRepository = mensagemRepository;
        }

        [Authorize]
        [HttpGet("{usuarioUmId}/{usuarioDoisId}", Name = "MensagemObter")]
        public ActionResult Obter(string usuarioUmId, string usuarioDoisId, [FromHeader(Name = "Accept")]string mediaType) //vai receber do cabeçalho o tipo de mídia para determinar se quer ou não os hyperlinks
        {
            if(usuarioUmId == usuarioDoisId)
               return UnprocessableEntity();

            //primeiro obtem todas as mensagens entra os dois usuarios
            var mensagens = _mensagemRepository.ObterMensagens(usuarioUmId, usuarioDoisId);


            //se a rquisição for do tipo talkto vai retornar a lista com os links, caso contrário, retorna sem
            if (mediaType == CustomMediaType.Hateoas)
            {               
                //converte a lista de Mensagem para MensagemDTO
                var listaMsg = _mapper.Map<List<Mensagem>, List<MensagemDTO>>(mensagens);

                //tranforma  a lista de Mensagem DTO em ListaDTO para ela ter o atributo Links nela como um todo
                var lista = new ListaDTO<MensagemDTO>() { Lista = listaMsg };

                //adiciona os links dela
                lista.Links.Add(new LinkDTO("_self", Url.Link("MensagemObter", new { usuarioUmId = usuarioUmId, usuarioDoisId = usuarioDoisId }), "GET"));

                return Ok(lista);

            }
            else            
                return Ok(mensagens);
           

            

        }

        [Authorize]
        [HttpPost("", Name = "MensagemCadastrar")]
        public ActionResult Cadastrar([FromBody] Mensagem mensagem, [FromHeader(Name = "Accept")] string mediaType)
        {

            if (ModelState.IsValid)
            {
                try
                {
                    _mensagemRepository.Cadastrar(mensagem);

                    if (mediaType == CustomMediaType.Hateoas)
                    {
                        //converte a  Mensagem para MensagemDTO
                        var mensagemDTO = _mapper.Map<Mensagem, MensagemDTO>(mensagem);

                        //Adiciona os links a essa mensagem, relativo a ele mesmo
                        mensagemDTO.Links.Add(new LinkDTO("_self", Url.Link("MensagemCadastrar", null), "POST"));
                        mensagemDTO.Links.Add(new LinkDTO("_atualizacaoParcial", Url.Link("MensagemAtualizacaoParcial", new { id = mensagem.Id }), "PATCH"));
                        return Ok(mensagemDTO);
                    }
                    else
                        return Ok(mensagem);
                   
                   

                }
                catch(Exception e)
                {
                    return UnprocessableEntity(e);
                }
            }
            else
                return UnprocessableEntity(ModelState);                        
        }

        //utilização do PATCH pq essa rota só altarea alguns campos da entidade
        //JsonPatch - é um objeto q tem 3 campos principais: "op":"add|remove|replace" , "path": "texto|excluido".., "value": "novo texto|novadata..."
        //o op recebe a ação, o path recebe o campo q vai sofrer a ação e o value é o novo valor que o campo vai receber
        [Authorize]
        [HttpPatch("{id}", Name = "MensagemAtualizacaoParcial") ]
        public ActionResult AtualizacaoParcial(int id, [FromBody]JsonPatchDocument<Mensagem> jsonPatch, [FromHeader(Name = "Accept")] string mediaType)
        {
            if (jsonPatch == null)
                return BadRequest();

            var mensagem = _mensagemRepository.Obter(id);

            jsonPatch.ApplyTo(mensagem);
            mensagem.Atualizado = DateTime.UtcNow;

            _mensagemRepository.Atualizar(mensagem);


            if (mediaType == CustomMediaType.Hateoas)
            {
                //converte a  Mensagem para MensagemDTO
                var mensagemDTO = _mapper.Map<Mensagem, MensagemDTO>(mensagem);

                //Adiciona os links a essa mensagem, relativo a ele mesmo
                mensagemDTO.Links.Add(new LinkDTO("_self", Url.Link("MensagemAtualizacaoParcial", new { id = mensagem.Id }), "PATCH"));

                return Ok(mensagemDTO);

            }
            else
                return Ok(mensagem);


        }


    }
}
