using Microsoft.AspNetCore.Mvc;
using app.Data;
using app.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
namespace app.Controllers
{
    [ApiController]
    [Route("/api/contratos")]
    public class ContratoController : ControllerBase
    {
        private readonly IMemoryCache _cache;

        public ContratoController(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

        [HttpGet]
        [Route("")]
        public async Task<ActionResult<List<Contrato>>> Get([FromServices] DataContext context)
        {
            var cacheKey = "contratos";
            if (!_cache.TryGetValue(cacheKey, out List<Contrato> contratos))
            {
                contratos = await context.Contratos.Include(c => c.Prestacoes).ToListAsync();
                var cacheExpiryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.Today.AddDays(1),
                    Priority = CacheItemPriority.High,
                    SlidingExpiration = TimeSpan.FromMinutes(2)
                };
                _cache.Set(cacheKey, contratos, cacheExpiryOptions);
            }
            return new JsonResult(contratos);

        }

        [HttpPost]
        [Route("")]
        public async Task<ActionResult<Contrato>> Post(
            [FromServices] DataContext context,
            [FromBody] Contrato model)
        {
            if (ModelState.IsValid)
            {
                context.Contratos.Add(model);
                await context.SaveChangesAsync();
                var valor = (model.ValorFinanciado / model.Parcelas);
                for (int i = 0; i < model.Parcelas; i++)
                {
                    Prestacao prestacao = new Prestacao();
                    prestacao.Valor = valor;
                    prestacao.ContratoId = model.Id;
                    prestacao.DataVencimento = DateTime.Today.AddMonths(i);
                    context.Prestacoes.Add(prestacao);
                }
                await context.SaveChangesAsync();
                return model;
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        [HttpGet]
        [Route("{id:int}")]
        public async Task<ActionResult<Contrato>> GetById([FromServices] DataContext context, int id)
        {
            var cacheKey = "contrato";
            if (!_cache.TryGetValue(cacheKey, out Contrato contrato))
            {
                contrato = await context.Contratos.Include(c => c.Prestacoes).FirstOrDefaultAsync(c => c.Id == id);
                var cacheExpiryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.Today.AddDays(1),
                    Priority = CacheItemPriority.High,
                    SlidingExpiration = TimeSpan.FromMinutes(2)
                };
                _cache.Set(cacheKey, contrato, cacheExpiryOptions);
            }
            return new JsonResult(contrato);

        }

        // [Route("prestacoes/{id:int}")]
        // public async Task<ActionResult<List<Prestacao>>> GetByPrestacao([FromServices] DataContext context, int id)
        // {
        //     //var prestacoes = await context.Prestacoes.Where(x => x.ContratoId == id).ToListAsync();
        //     return new JsonResult(prestacoes);

        // }

    }
}