using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder =>
        {
            builder.WithOrigins("http://localhost:4200")
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.Configure<JsonOptions>(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Habilitar CORS antes de la autorización
app.UseCors("AllowSpecificOrigin");

app.UseAuthorization();

app.MapRazorPages();

List<Viaje> viajes = new List<Viaje>();
List<Peticion> peticiones = new List<Peticion>();
List<Conductor> conductores = new List<Conductor>();

//AGREGACION DATOS VIAJES
viajes.Add(new Viaje(1, 1, 3, 0, Ciudades.MADRID, Ciudades.ALCALA, EstadoViaje.ESPERANDO));
viajes.Add(new Viaje(2, 1, 3, 0, Ciudades.MADRID, Ciudades.TORREJON, EstadoViaje.ESPERANDO));
viajes.Add(new Viaje(3, 2, 5, 0, Ciudades.MADRID, Ciudades.ALCALA, EstadoViaje.ESPERANDO));
viajes.Add(new Viaje(4, 3, 3, 0, Ciudades.ALCALA, Ciudades.TORREJON, EstadoViaje.ESPERANDO));
viajes.Add(new Viaje(5, 4, 3, 0, Ciudades.GUADALAJARA, Ciudades.COSLADA, EstadoViaje.ESPERANDO));
int nextViajeId = 6;
//AGREGACION DATOS CONDUCTORES
conductores.Add(new Conductor(1, "Juanjo"));
conductores.Add(new Conductor(2, "María"));
conductores.Add(new Conductor(3, "Daniel"));
conductores.Add(new Conductor(4, "Elías"));

int nextPeticionId = 1;

foreach (Conductor conductor in conductores)
{
    conductor.viajes = [];
    foreach (Viaje viaje in viajes)
    {
        
        if(viaje.conductor == conductor.id)
        {
            conductor.viajes.Add(viaje);
        }
    }
}

var ViajesRoute = app.MapGroup("/viajes");
var ConductoresRoute = app.MapGroup("/conductores");
var PeticionesRoute = app.MapGroup("/peticiones");

//RUTA VIAJES
ViajesRoute.MapGet("/", () =>
{
    foreach (Conductor conductor in conductores)
    {
        ActualizarPeticionesViaje(conductor);
    }
    return viajes;
});

//RUTA CONDUCTORES
ConductoresRoute.MapGet("/", () =>
{
    foreach (Conductor conductor in conductores)
    {
        ActualizarPeticionesViaje(conductor);
    }
    return conductores;
});
ConductoresRoute.MapPost("/{conductorId:int}/viajes", (int conductorId,Viaje viaje) =>
{
    viaje.id = nextViajeId++;
    viaje.estado = EstadoViaje.ESPERANDO;
    viaje.pasajeros = 0;
    Conductor conductor = conductores.Find(conductor => conductor.id == conductorId);
    if (conductor is null)
    {
        return Results.NotFound("Conductor no encontrado");
    }
    var viajeNuevo = conductor.agregarViaje(viaje);
    viajes.Add(viajeNuevo);
    return Results.Ok(viajeNuevo);
});
ConductoresRoute.MapGet("/{conductorId:int}/viajes", (int conductorId) =>
{
    Conductor conductor = conductores.Find(conductor => conductor.id == conductorId);
    if (conductor is null)
    {
        return Results.NotFound("Conductor no encontrado");
    }
    if(conductor.viajes.Count == 0)
    {
        return Results.Ok("Este conductor no tiene viajes para poder recibir peticiones");
    }
    ActualizarPeticionesViaje(conductor);

    return Results.Ok(conductor.viajes);
    
});
ConductoresRoute.MapPut("/{conductorId}/viajes/{viajeId}", (int conductorId, int viajeId, [FromBody] ActionRequest action) =>
{
        Conductor conductor = conductores.Find(conductor => conductor.id == conductorId);
        if (conductor is null)
        {
            return Results.NotFound("Conductor no encontrado");
        }
        Viaje viaje = conductor.viajes.Find(viaje => viaje.id == viajeId);
        if (viaje is null)
        {
            return Results.NotFound("Viaje no encontrado");
        }
    if(action.Action == "aceptar")
    {
        ActualizarPeticionesViaje(conductor);
        if (conductor.viajes.Find(viaje => viaje.id == viajeId).peticiones.Count == 0)
        {
            return Results.BadRequest("No tienes peticiones activas que aceptar en este viaje");
        }
        int peticionId = conductor.AceptarPeticion(viajeId);
        return Results.Ok(peticiones.Find(peticion => peticion.id == peticionId));
    }else if(action.Action == "viajar")
    {
        foreach (var v in conductor.viajes)
        {
            if (v.estado == EstadoViaje.VIAJANDO)
            {
                return Results.BadRequest("No puedes comenzar a viajar sin haber terminado tu actual viaje.");
            }
        }
        if (viaje.pasajeros <= 0)
        {
            return Results.BadRequest("No puedes viajar sin pasajeros.");
        }
        conductor.ComenzarViaje(viajeId);
        return Results.Ok(conductor.viajes.Find(viaje => viaje.id == viajeId));
    }
    else
    {
        return Results.BadRequest("Accion invalida.");
    }
});

//RUTA PETICIONES
PeticionesRoute.MapGet("/", () =>
{
    return peticiones is not null ? Results.Ok(peticiones) : Results.NotFound("No hay peticiones todavía");
});
PeticionesRoute.MapPost("/", (Peticion peticion) =>
{
    if(peticion.pasajeros < 0)
    {
        return Results.BadRequest("La petición no pudo ser procesada");
    }
    peticion.estado = EstadoPeticion.ESPERANDO;
    peticion.id = nextPeticionId++;
    peticiones.Add(peticion);
    return Results.Ok(peticion);
});
PeticionesRoute.MapPut("/", (Peticion peticion) =>
{
    int peticionId = peticion.id;
    var peticionExiste = peticiones.Find(peticion => peticion.id == peticionId);
    if (peticion is null || peticionExiste.nombre != peticion.nombre)
    {
        return Results.NotFound("No se ha encontrado dicha peticion");
    }
    if (peticion.estado == EstadoPeticion.ACEPTADA)
    {
        var viaje = viajes.Find(viaje => viaje.id == peticion.viajeId);
        if (viaje.estado == EstadoViaje.ESPERANDO)
        {
            var conductor = conductores.Find(conductor => conductor.id == viaje.conductor);
            conductor.PeticionCancelada(viaje.id, peticion.pasajeros);
        }
        else
        {
            return Results.BadRequest("No se puede cancelar esta petición. Viaje en curso o terminado.");
        }
    }else if(peticion.estado == EstadoPeticion.CANCELADA)
    {
        return Results.BadRequest("Peticion ya está cancelada");
    }
    peticion.CancelarPeticion();
    peticiones = peticiones.FindAll(peticion => peticion.id != peticionId);
    peticiones.Add(peticion);
    return Results.Ok(peticion);
});


app.Run();


void ActualizarPeticionesViaje(Conductor conductor)
{
    foreach (Viaje viaje in conductor.viajes)
    {
        viaje.peticiones = [];
        foreach (Peticion peticion in peticiones)
        {
            if (peticion.origen == viaje.origen && peticion.destino == viaje.destino && peticion.pasajeros <= (viaje.maxCapacidad - viaje.pasajeros) && viaje.estado == EstadoViaje.ESPERANDO && peticion.estado == EstadoPeticion.ESPERANDO)
            {
                viaje.peticiones.Add(peticion);
            }
        }
    }
}


public enum EstadoViaje {
    ESPERANDO, VIAJANDO, TERMINADO
}

public enum EstadoPeticion
{
    ESPERANDO, ACEPTADA, CANCELADA
}

public enum Ciudades
{
    MADRID, ALCALA, TORREJON, COSLADA, GUADALAJARA, SAN_FERNANDO
}

public class Viaje
{
    public int id { get; set; }
    public int conductor { get; set; }
    public int maxCapacidad { get; set; }
    public int pasajeros { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Ciudades destino { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Ciudades origen { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EstadoViaje estado { get; set; }

    public List<Peticion>? peticiones { get; set; }

    public Viaje(int id, int conductor, int maxCapacidad, int pasajeros, Ciudades origen, Ciudades destino, EstadoViaje estado)
    {
        this.conductor = conductor;
        this.id = id;
        this.maxCapacidad = maxCapacidad;
        this.pasajeros = pasajeros;
        this.origen = origen;
        this.destino = destino;
        this.estado = estado;
        this.peticiones = [];
    }
}

public class Conductor
{
    public int id { get; set; }
    public string nombre { get; set; }
    public List<Viaje> viajes { get; set; }

    public Conductor(int id, string nombre)
    {
        this.id = id;
        this.nombre = nombre;
    }

    public Viaje agregarViaje(Viaje viaje)
    {
        Viaje nuevo = new Viaje(viaje.id, this.id, viaje.maxCapacidad, viaje.pasajeros, viaje.origen, viaje.destino, viaje.estado);
        this.viajes.Add(nuevo);
        return nuevo;
    }

    public int AceptarPeticion(int viajeId)
    {
        int peticionId = 0;
        this.viajes.ForEach(viaje =>
        {
            if (viaje.id == viajeId)
            {
                viaje.peticiones[0].estado = EstadoPeticion.ACEPTADA;
                viaje.pasajeros += viaje.peticiones[0].pasajeros;
                viaje.peticiones[0].viajeId = viaje.id;
                peticionId = viaje.peticiones[0].id;
            }
        });
        return peticionId;
    }

    public void PeticionCancelada(int viajeId, int pasajerosPeticion)
    {
        this.viajes.ForEach(viaje =>
        {
            if(viaje.id == viajeId)
            {
                viaje.pasajeros -= pasajerosPeticion;
            }
        });
    }

    public void ComenzarViaje(int viajeId)
    {
       foreach(var viaje in this.viajes)
        {
            if(viaje.id == viajeId)
            {
                viaje.estado = EstadoViaje.VIAJANDO;
                _ = Task.Run(async () =>
                {
                    await Task.Delay(30000);
                    viaje.estado = EstadoViaje.TERMINADO;

                });
            }
        }
    }
}

public class Peticion
{
    public int id { get; set; }
    public string nombre { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Ciudades origen { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Ciudades destino { get; set; }
    public int pasajeros { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EstadoPeticion estado { get; set; }

    public int? viajeId { get; set; }

    public void CancelarPeticion()
    {
        this.estado = EstadoPeticion.CANCELADA;
    }
}

public class ActionRequest
{
    public string Action { get; set; }
}