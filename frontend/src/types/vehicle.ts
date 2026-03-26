export const VehicleType = {
  Unknown: 0,
  Tram: 1,
  Metro: 2,
  Train: 3,
  Bus: 4,
  Ferry: 5,
  CableCar: 6,
  Gondola: 7,
  Funicular: 8,
} as const

export type VehicleType = (typeof VehicleType)[keyof typeof VehicleType]

export interface VehicleSnapshot {
  vehicleId: string
  latitude: number
  longitude: number
  bearing: number | null
  speed: number | null
  vehicleType: VehicleType
  routeShortName: string | null
  headsign: string | null
  agencyName: string | null
  delaySeconds: number | null
  timestamp: string
}
