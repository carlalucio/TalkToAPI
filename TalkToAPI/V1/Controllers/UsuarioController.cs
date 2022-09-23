using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System;
using TalkToAPI.V1.Models;
using TalkToAPI.V1.Repositories.Contracts;
using TalkToAPI.V1.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using AutoMapper;
using System.Linq;
using TalkToAPI.Helpers.Constantes;
using Microsoft.AspNetCore.Cors;

namespace TalkToAPI.V1.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    [EnableCors("AnyOrigin")] //habilita a politica especifica q eu criei
    public class UsuarioController : Controller
    {

        private readonly IMapper _mapper; //obj para injeção de dependência da interface AutoMapper
        private readonly IUsuarioRepository _usuarioRepository; //obj para injeção de dependência da interface Usuario
        private readonly ITokenRepository _tokenRepository;     //obj para injeção de dependência da interface Token
        private readonly UserManager<ApplicationUser> _userManager;    //obj para injeção de dependência do UserManeger
        private readonly IConfiguration _config;    //obj para injeçao utilização da chave jwt que está no aquivo de configuração appsettings.json
       

        public UsuarioController(IMapper mapper,IUsuarioRepository usuarioRepository, UserManager<ApplicationUser> userManager, IConfiguration config, ITokenRepository tokenRepository)
        {
            _mapper = mapper;   
            _usuarioRepository = usuarioRepository;           
            _userManager = userManager;
            _config = config;
            _tokenRepository = tokenRepository;
            
        }

        [Authorize]
        [HttpGet("", Name = "UsuarioObterTodos")]
        [DisableCors()] //desabilita essa rota especificamente pelo cors
        public ActionResult ObterTodos([FromHeader(Name = "Accept")] string mediaType)
        {
            //obtem todos ApplicationUser do DB e converte em Lista
            var usuariosAppUser = _userManager.Users.ToList();


            if (mediaType == CustomMediaType.Hateoas)
            {
                //converte a lista de AppUser em lista de UsuarioDTO
                var listaUsuarioDTO = _mapper.Map<List<ApplicationUser>, List<UsuarioDTO>>(usuariosAppUser);

                //cada UsuarioDTO dessa lista recebe um novo Link relativo a ele mesmo usando a rota individual
                foreach (var usuarioDTO in listaUsuarioDTO)
                {
                    usuarioDTO.Links.Add(new LinkDTO("_self", Url.Link("UsuarioObter", new { id = usuarioDTO.Id }), "GET"));
                }

                //a lista com todos os usuarios tbm receve um link para ela mesma no final
                var lista = new ListaDTO<UsuarioDTO>() { Lista = listaUsuarioDTO };
                lista.Links.Add(new LinkDTO("_self", Url.Link("UsuarioObterTodos", null), "GET"));

                return Ok(lista);
            }
            else
            {
                //trnforma o appuser em um ususario sem expor os dados confidenciais
                var usuarioResult = _mapper.Map<List<ApplicationUser>, List<UsuarioDTOSemHyperlink>>(usuariosAppUser);
                return Ok(usuarioResult);
            }               
            
        }

        [HttpGet("{id}", Name = "UsuarioObter")]
        public ActionResult ObterUsuario(string id, [FromHeader(Name = "Accept")] string mediaType)
        {
            //obtem o ApplicationUser do DB
            var usuario = _userManager.FindByIdAsync(id).Result;
            if (usuario == null)
                return NotFound();

            if (mediaType == CustomMediaType.Hateoas)
            {
                ///converte um ApplicationUser em UsuarioDTO
                var usuarioDTOdb = _mapper.Map<ApplicationUser, UsuarioDTO>(usuario);

                //Adiciona os links a esse usuário, relativo a ele mesmo
                usuarioDTOdb.Links.Add(new LinkDTO("_self", Url.Link("UsuarioObter", new { id = usuario.Id }), "GET"));
                usuarioDTOdb.Links.Add(new LinkDTO("_atualizar", Url.Link("UsuarioAtualizar", new { id = usuario.Id }), "PUT"));
                return Ok(usuarioDTOdb);
            }
            else
            {
                var usuarioResult = _mapper.Map<ApplicationUser, UsuarioDTOSemHyperlink>(usuario);
                return Ok(usuarioResult);
            }
                
        }
        
              

        [HttpPost("", Name = "UsuarioCadastrar")]
        public ActionResult Cadastrar([FromBody] UsuarioDTO usuarioDTO, [FromHeader(Name = "Accept")] string mediaType)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser usuario = _mapper.Map<UsuarioDTO, ApplicationUser>(usuarioDTO);
                usuario.FullName = usuarioDTO.Nome;
                usuario.UserName = usuarioDTO.Email;
                usuario.Email = usuarioDTO.Email;
                

                var resultado = _userManager.CreateAsync(usuario, usuarioDTO.Senha).Result;

                if (!resultado.Succeeded)
                {
                    List<string> erros = new List<string>();
                    foreach (var erro in resultado.Errors)
                    {
                        erros.Add(erro.Description);
                    }
                    return UnprocessableEntity(erros);
                }
                else
                {
                    if (mediaType == CustomMediaType.Hateoas)
                    {
                        //converte um ApplicationUser em UsuarioDTO
                        var usuarioDTOdb = _mapper.Map<ApplicationUser, UsuarioDTO>(usuario);

                        //Adiciona os links a esse usuário, relativo a ele mesmo
                        usuarioDTOdb.Links.Add(new LinkDTO("_self", Url.Link("UsuarioCadastrar", new { id = usuario.Id }), "POST"));
                        usuarioDTOdb.Links.Add(new LinkDTO("obter", Url.Link("UsuarioObter", new { id = usuario.Id }), "GET"));
                        usuarioDTOdb.Links.Add(new LinkDTO("_atualizar", Url.Link("UsuarioAtualizar", new { id = usuario.Id }), "PUT"));

                        return Ok(usuarioDTOdb);
                    }
                    else
                    {
                        var usuarioResult = _mapper.Map<ApplicationUser, UsuarioDTOSemHyperlink>(usuario);
                        return Ok(usuarioResult);
                    }
                                            
                }                   
            }
            else
                return UnprocessableEntity(ModelState);
        }

        //rota para atualização do usuario cadastrado
        [Authorize]
        [HttpPut("{id}", Name = "UsuarioAtualizar")]
        public ActionResult Atualizar(string id, [FromBody] UsuarioDTO usuarioDTO, [FromHeader(Name = "Accept")] string mediaType)
        {
            //validação para pegar o usuario logado e só permitir q o usuário edite o próprio cadastro
            ApplicationUser usuario = _userManager.GetUserAsync(HttpContext.User).Result;

            if ( usuario.Id != id)
                    return Forbid();

            if (ModelState.IsValid)
            {
                               
                usuario.FullName = usuarioDTO.Nome;
                usuario.UserName = usuarioDTO.Email;
                usuario.Email = usuarioDTO.Email;
                usuario.Slogan = usuarioDTO.Slogan;

                //TODO - remover no Identity critérios da senha
                var resultado = _userManager.UpdateAsync(usuario).Result;
                _userManager.RemovePasswordAsync(usuario);
                _userManager.AddPasswordAsync(usuario, usuarioDTO.Senha);

                if (!resultado.Succeeded)
                {
                    List<string> erros = new List<string>();
                    foreach (var erro in resultado.Errors)
                    {
                        erros.Add(erro.Description);
                    }
                    return UnprocessableEntity(erros);
                }
                else
                {
                    if (mediaType == CustomMediaType.Hateoas)
                    {
                        //converte um ApplicationUser em UsuarioDTO
                        var usuarioDTOdb = _mapper.Map<ApplicationUser, UsuarioDTO>(usuario);

                        //Adiciona os links a esse usuário, relativo a ele mesmo
                        usuarioDTOdb.Links.Add(new LinkDTO("_self", Url.Link("UsuarioAtualizar", new { id = usuario.Id }), "PUT"));
                        usuarioDTOdb.Links.Add(new LinkDTO("obter", Url.Link("UsuarioObter", new { id = usuario.Id }), "GET"));

                        return Ok(usuarioDTOdb);
                    }
                    else
                    {
                        var usuarioResult = _mapper.Map<ApplicationUser, UsuarioDTOSemHyperlink>(usuario);
                        return Ok(usuarioResult);
                    }                                           
                }                    
            }
            else
                return UnprocessableEntity(ModelState);
        }

        [HttpPost("login")]
        public ActionResult Login([FromBody] UsuarioDTO usuarioDTO)
        {
            //remove os campos nome e confirmação de senha, pois o login só usa email e senha
            ModelState.Remove("Nome");
            ModelState.Remove("ConfirmacaoSenha");

            if (ModelState.IsValid)
            {
                ApplicationUser usuario = _usuarioRepository.Obter(usuarioDTO.Email, usuarioDTO.Senha);
                if (usuario != null)
                {
                    //login com JWT retorna o token criado
                    return GerarToken(usuario);
                }
                else
                    return NotFound("Usuário não localizado!");
            }
            else
                return UnprocessableEntity(ModelState);

        }


        [HttpPost("renovar")]
        public ActionResult Renovar([FromBody] TokenDTO tokenDTO)
        {
            var refreshTokenDB = _tokenRepository.Obter(tokenDTO.RefreshToken);

            if (refreshTokenDB == null)
                return NotFound();

            //pegar o RefreshToken antigo e atualizar ele - desativar o refresh token
            refreshTokenDB.Atualizado = DateTime.Now;
            refreshTokenDB.Utilizado = true;
            _tokenRepository.Atualizar(refreshTokenDB);

            //Gerar um novo Token/RefreshToken e salvar
            var usuario = _usuarioRepository.Obter(refreshTokenDB.UsuarioId);

            return GerarToken(usuario);

        }


        private TokenDTO BuildToken(ApplicationUser usuario)
        {
            //array de Claims
            var claims = new[]
            {
                //identifica o usuario pelo email
                new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id)
            };
            //cria a chave e usa o Encoding pq precisa transformar em um array de Bytes            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"])); //chave dentro do appsetting.json


            //cria a assinatura passando o algoritmo usado para criptografia
            var sign = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            //criar data de expiração do token. Usa o UtcNow para ele adicionar independente do fuso horario do usuario
            var exp = DateTime.UtcNow.AddHours(1);

            //instancia da classe que vai gerar o token. Dentro do construtor passamos os elementos
            JwtSecurityToken token = new JwtSecurityToken(
                issuer: null,                    //indica o emissor do token
                audience: null,                 //indica o dominio que vai consumir esse token
                claims: claims,                 //claims com identificador do usuario
                expires: exp,                   //validade
                signingCredentials: sign        //
                );

            //colocar em uma variavel a instancia do JwtSecurityTokenHandler que recebe as informações do token e gera uma string com a criptografia
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = Guid.NewGuid().ToString();
            var expRefreshToken = DateTime.UtcNow.AddHours(2);
            var tokenDTO = new TokenDTO { Token = tokenString, Expiration = exp, ExpirationRefreshToken = expRefreshToken, RefreshToken = refreshToken };

            return tokenDTO;
        }

        private ActionResult GerarToken(ApplicationUser usuario)
        {
            //gera o token
            var token = BuildToken(usuario);

            //salvar o Token no banco
            var tokenModel = new Token()
            {
                RefreshToken = token.RefreshToken,
                ExpirationToken = token.Expiration,
                ExpirationRefreshToken = token.ExpirationRefreshToken,
                Usuario = usuario,
                Criado = DateTime.Now,
                Utilizado = false

            };
            _tokenRepository.Cadastrar(tokenModel);
            return Ok(token);
        }
    }
}
