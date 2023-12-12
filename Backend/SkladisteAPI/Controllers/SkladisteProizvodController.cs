﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkladisteAPI.Data;
using SkladisteAPI.Model.Entities;
using System.Diagnostics.Eventing.Reader;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkladisteAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SkladisteProizvodController : ControllerBase
    {
        private readonly SkladisteDbContext skladistaDbContext;

        public SkladisteProizvodController(SkladisteDbContext skladistaDbContext)
        {
            this.skladistaDbContext = skladistaDbContext;
        }

        [HttpGet]
        [Route("{id}")]

        public async Task<IActionResult> DohvatiSkladisteProizvode([FromRoute] int id)
        {
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve

            };

            var skladisteProizvodi = await skladistaDbContext.SkladisteProizvodi
            .Include(sp => sp.Proizvod)
            .Where(sp => sp.SkladisteID == id)
            .Select(sp => new {
                sp.ID,
                sp.Kolicina,
                Proizvod = new
                {
                    sp.Proizvod.ID,
                    sp.Proizvod.Naziv,
                    sp.Proizvod.Kategorija,
                    sp.Proizvod.Cena
                }
            }).ToListAsync();

            return Ok(skladisteProizvodi);
        }

        private IActionResult Ok(List<SkladisteProizvod> skladisteProizvodi, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [Route("{id}")]

        public async Task<IActionResult> DodajSkladisteProizvod([FromRoute] int id, [FromBody] SkladisteProizvod skladisteProizvod)
        {
            var postojecaVeza = await skladistaDbContext.SkladisteProizvodi
                .SingleOrDefaultAsync(sp => sp.SkladisteID == id && sp.ProizvodID == skladisteProizvod.ProizvodID);

            var skladiste = await skladistaDbContext.Skladista.Include(s => s.SkladisteProizvodi).FirstOrDefaultAsync(s => s.ID == id);

            if (postojecaVeza != null)
            {
                if (skladiste != null)
                {
                    if (skladiste.Popunjeno + skladisteProizvod.Kolicina <= skladiste.Kapacitet)
                    {
                        postojecaVeza.Kolicina += skladisteProizvod.Kolicina;

                    }
                    else
                    {
                        return NotFound();
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {

                SkladisteProizvod novaVeza = new()
                {
                    SkladisteID = id,
                    ProizvodID = skladisteProizvod.ProizvodID,
                    Kolicina = skladisteProizvod.Kolicina
                };

                skladistaDbContext.SkladisteProizvodi.Add(novaVeza);
            }
            await skladistaDbContext.SaveChangesAsync();

            if (skladiste != null)
            {
                skladiste.AzurirajPopunjeno();
                await skladistaDbContext.SaveChangesAsync();
            }

            return Ok();
        }
        [HttpPut]
        [Route("{id}")]

        public async Task<IActionResult> IsporuciSkladisteProizvod([FromRoute] int id, [FromBody] SkladisteProizvod skladisteProizvod)
        {
            var proizvod = await skladistaDbContext.SkladisteProizvodi
                .SingleOrDefaultAsync(sp => sp.SkladisteID == id && sp.ProizvodID == skladisteProizvod.ProizvodID);

            if(proizvod == null)
            {
                return NotFound();
            }
            if(proizvod.Kolicina <= skladisteProizvod.Kolicina)
            {
                return BadRequest();
            }
            proizvod.Kolicina -= skladisteProizvod.Kolicina;
            await skladistaDbContext.SaveChangesAsync();

            var skladiste = await skladistaDbContext.Skladista.Include(s => s.SkladisteProizvodi).FirstOrDefaultAsync(s => s.ID == id);
            if (skladiste == null)
            {
                return NotFound();
            }
            skladiste.AzurirajPopunjeno();
            await skladistaDbContext.SaveChangesAsync();
            return Ok();

        }

        [HttpDelete]
        [Route("{id}")]

        public async Task<IActionResult> ObrisiSkladisteProizvod([FromRoute] int id, [FromBody] SkladisteProizvod skladisteProizvod)
        {
            var proizvod = await skladistaDbContext.SkladisteProizvodi
                .SingleOrDefaultAsync(sp => sp.SkladisteID == id && sp.ProizvodID == skladisteProizvod.ProizvodID);

            if(proizvod == null)
            {
                return NotFound();
            }
            skladistaDbContext.SkladisteProizvodi.Remove(proizvod);
            await skladistaDbContext.SaveChangesAsync();

            var skladiste = await skladistaDbContext.Skladista.Include(s => s.SkladisteProizvodi).FirstOrDefaultAsync(s => s.ID == id);
            if (skladiste == null)
            {
                return NotFound();
            }
            skladiste.AzurirajPopunjeno();
            await skladistaDbContext.SaveChangesAsync();

            return Ok();

        }
    }

    }

