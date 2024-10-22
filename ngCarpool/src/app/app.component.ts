import { NgClass } from '@angular/common';
import { Component, inject, OnInit, signal, WritableSignal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterOutlet } from '@angular/router';
import { Conductor, Peticion, RestService, Viaje } from './rest.service';

interface Error{
  msg: string,
  state: boolean
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NgClass, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit{

  ciudades: string[] = ['MADRID','GUADALAJARA','ALCALA','SAN_FERNANDO','COSLADA','TORREJON'];
  ciudadesDestino: WritableSignal<string[]> = signal([]);
  ciudadesDestinoViaje: WritableSignal<string[]> = signal([]);

  origen: string = '';
  destino: string = '';
  pasajeros: number = 0;
  nombre: string = '';
  conductor: number = 0;
  origenViaje: string = '';
  destinoViaje: string = '';
  maxCapacidad: number = 0;

  restService = inject(RestService);
  
  viajes: Viaje[] = [];
  conductores: Conductor[] = [];
  peticiones: Peticion[] = []

  error: Error = {msg:'',state:false}

  ngOnInit(): void {
    this.updateInfo();
  }

  getViajes(){
    this.restService.getViajes().subscribe({
      next: (res) => {
        this.viajes = res;
        console.log(res)
      }
    });
  }

  getConductores(){
    this.restService.getConductores().subscribe({
      next: (res) => {
        this.conductores = res;
        console.log(res)
      }
    });
  }

  getPeticiones(){
    this.restService.getPeticiones().subscribe({
      next: (res) => {
        this.peticiones = res;
        console.log(res)
      }
    });
  }

  updateInfo(){
    this.getConductores();
    this.getViajes();
    this.getPeticiones();
  }

  accionViaje(viajeId: number,conId: number,accion:string){
    this.restService.accionViaje(viajeId,conId,accion).subscribe({
      next: (res) => {
        console.log(res)
        this.updateInfo();
      },
      error: (err) => {
        this.error.msg = err.error;
        this.error.state = true;
        setTimeout(() => {
          this.error.msg = '';
          this.error.state = false;
        }, 2000)
        console.log(err);
      }
    });
  }

  sendSol(){
    this.restService.addPeticion({origen: this.origen,destino: this.destino,pasajeros: this.pasajeros,nombre: this.nombre}).subscribe({
      next: (res) => {
        console.log(res);
        this.updateInfo();
      },
      error: (err) => {
        this.error.msg = err.error;
        this.error.state = true;
        setTimeout(() => {
          this.error.msg = '';
          this.error.state = false;
        }, 2000)
        console.log(err);
      }
    });
  }

  updateDestino(origen: any, form: string){
    if(form == 'peticion'){
      this.ciudadesDestino.set(this.ciudades.filter(ciudad => ciudad != origen));
      console.log(origen)
    }else{
      this.ciudadesDestinoViaje.set(this.ciudades.filter(ciudad => ciudad != origen));
      console.log(this.ciudadesDestinoViaje())
    }
  }

  cancelarPeticion(pet: Peticion){
    this.restService.cancelarPeticion(pet).subscribe({
      next: (res) => {
        this.updateInfo();
        console.log(res)
      },
      error: (err) => {
        this.error.msg = err.error;
        this.error.state = true;
        setTimeout(() => {
          this.error.msg = '';
          this.error.state = false;
        }, 2000)
        console.log(err);
      }
    })
  }

  addViaje(){
    const viaje: Viaje = {
      maxCapacidad: this.maxCapacidad,
      origen: this.origenViaje,
      destino: this.destinoViaje,
      pasajeros: 0
    }
    this.restService.addViaje(this.conductor,viaje).subscribe({
      next: (res) => {
        this.updateInfo();
      },
      error: (err) => {
        this.error.msg = err.error;
        this.error.state = true;
        setTimeout(() => {
          this.error.msg = '';
          this.error.state = false;
        }, 2000)
        console.log(err);
      }
    });
  }

}
