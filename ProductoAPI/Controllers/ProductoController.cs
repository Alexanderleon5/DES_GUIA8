using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductoAPI.Models;
using StackExchange.Redis;

namespace ProductoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly ProductoContext _context;
        private readonly IConnectionMultiplexer _redis;

        public ProductosController(ProductoContext context, IConnectionMultiplexer redis)
        {
            _context = context;
            _redis = redis; 
        }

        // GET: api/Productos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductos()
        {
            
            var db = _redis.GetDatabase(); //Se crea instancia a base de datos de redis
            string cacheKey = "productoList"; //Se define la clave para almacenar la lista de productos
            var productosCache = await db.StringGetAsync (cacheKey);//Se obttiene la lista de productos cache
            if (!productosCache.IsNullOrEmpty)// Si la lista de producto es nula, se retorna a la lista de productos
            {
                return JsonSerializer.Deserialize<List<Producto>>(productosCache);   
            }
            var productos = await _context.Productos.ToListAsync();  // si la lista de productos ers nula se obetiene la lista de productos de la base de datos 
            await db.StringSetAsync (cacheKey, JsonSerializer.Serialize(productos), TimeSpan.FromMinutes(10));
            return productos;
        }

        // GET: api/Productos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Producto>> GetProducto(int id)
        {
            
            var db = _redis.GetDatabase(); //Se crea instancia a base de datos de redis
            string cacheKey = "producto_" + id.ToString(); //Se define la clave para almacenar la lista de productos
            var productoCache = await db.StringGetAsync(cacheKey);//Se obttiene la lista de productos cache
            if (!productoCache.IsNullOrEmpty)// Si la lista de producto es nula, se retorna a la lista de productos
            {
                return JsonSerializer.Deserialize<Producto>(productoCache);
            }
            var producto = await _context.Productos.FindAsync(id);  // si la lista de productos ers nula se obetiene la lista de productos de la base de datos 
            if (producto == null) 
            {
                return NotFound();
            
            
            }
            await db.StringSetAsync(cacheKey, JsonSerializer.Serialize(producto), TimeSpan.FromMinutes(10));
            return producto;
        }

        // PUT: api/Productos/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProducto(int id, Producto producto)
        {
            if (id != producto.Id)
            {
                return BadRequest();
            }

            _context.Entry(producto).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                var db = _redis.GetDatabase();  
                string cacheKeyProducto = "Prodcuto_" + id.ToString();
                string cacheKeyList = "productoList";
                await db.KeyDeleteAsync(cacheKeyProducto);  
                await db.KeyDeleteAsync(cacheKeyList);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductoExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Productos
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Producto>> PostProducto(Producto producto)
        {
            _context.ProductoSet.Add(producto);
            await _context.SaveChangesAsync();
            var db = _redis.GetDatabase();
            string cacheKeyList = "productoList";
            await db.KeyDeleteAsync(cacheKeyList);  
            return CreatedAtAction("GetProducto", new { id = producto.Id }, producto);
        }

        // DELETE: api/Productos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProducto(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();
            var db = _redis.GetDatabase();
            string cacheKeyProducto = "Producto_" + id.ToString();
            string cacheKeyList = "productoList";
            await db.KeyDeleteAsync(cacheKeyProducto);
            await db.KeyDeleteAsync(cacheKeyList);
            return NoContent();
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.Id == id);
        }
    }
}
