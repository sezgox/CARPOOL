import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface Viaje{
  origen: string,
  destino: string,
  maxCapacidad: number,
  pasajeros: number,
  id?: number,
  conductorId?: number,
  peticiones?: Peticion[],
  estado: string
}

export interface Peticion{
  origen: string,
  destino:string,
  pasajeros:number,
  nombre:string,
  id?: number,
  viajeId?: number
}

export interface Conductor{
  id: number,
  viajes: Viaje[],
  nombre: string
}

@Injectable({
  providedIn: 'root'
})

export class RestService {

  url: string = 'http://localhost:5072';
  conductores: string = '/conductores';
  peticiones: string = '/peticiones';
  viajes: string = '/viajes';

  constructor() { }

  http = inject(HttpClient);

  getViajes():Observable<Viaje[]>{
    return this.http.get<Viaje[]>(`${this.url}${this.viajes}`);
  }

  getConductores():Observable<Conductor[]>{
    return this.http.get<Conductor[]>(`${this.url}${this.conductores}`);
  }

  addViaje(conductorId: number, datosViaje:Viaje){
    return this.http.post(`${this.url}${this.conductores}/${conductorId}${this.viajes}`, datosViaje);
  }

  addPeticion(datosPeticion: Peticion){
    return this.http.post(`${this.url}${this.peticiones}`, datosPeticion);
  }

  cancelarPeticion(peticion: Peticion){
    return this.http.put(`${this.url}${this.peticiones}`, peticion);
  }

  accionViaje(viajeId: number, conductorId: number, action: string):Observable<Viaje | Peticion>{
    return this.http.put<Viaje | Peticion>(`${this.url}${this.conductores}/${conductorId}/viajes/${viajeId}`,{action});
  }

}
