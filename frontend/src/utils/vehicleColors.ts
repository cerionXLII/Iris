import { VehicleType } from '../types/vehicle'

// RGBA tuples for deck.gl [r, g, b, a]
export const VEHICLE_COLORS: Record<VehicleType, [number, number, number, number]> = {
  [VehicleType.Bus]:       [74,  158, 255, 220],  // #4A9EFF blue
  [VehicleType.Train]:     [255, 140, 66,  220],  // #FF8C42 orange
  [VehicleType.Metro]:     [176, 106, 255, 220],  // #B06AFF purple
  [VehicleType.Tram]:      [62,  255, 176, 220],  // #3EFFB0 green
  [VehicleType.Ferry]:     [0,   212, 255, 220],  // #00D4FF cyan
  [VehicleType.CableCar]:  [255, 215, 0,   220],  // gold
  [VehicleType.Gondola]:   [255, 182, 193, 220],  // light pink
  [VehicleType.Funicular]: [255, 99,  71,  220],  // tomato
  [VehicleType.Unknown]:   [150, 150, 150, 180],  // grey
}

export function getVehicleColor(type: VehicleType): [number, number, number, number] {
  return VEHICLE_COLORS[type] ?? VEHICLE_COLORS[VehicleType.Unknown]
}

export const VEHICLE_TYPE_LABELS: Record<VehicleType, string> = {
  [VehicleType.Bus]:       'Bus',
  [VehicleType.Train]:     'Train',
  [VehicleType.Metro]:     'Metro',
  [VehicleType.Tram]:      'Tram',
  [VehicleType.Ferry]:     'Ferry',
  [VehicleType.CableCar]:  'Cable Car',
  [VehicleType.Gondola]:   'Gondola',
  [VehicleType.Funicular]: 'Funicular',
  [VehicleType.Unknown]:   'Vehicle',
}

export const VEHICLE_TYPE_ICONS: Record<VehicleType, string> = {
  [VehicleType.Bus]:       '🚌',
  [VehicleType.Train]:     '🚆',
  [VehicleType.Metro]:     '🚇',
  [VehicleType.Tram]:      '🚊',
  [VehicleType.Ferry]:     '⛴️',
  [VehicleType.CableCar]:  '🚡',
  [VehicleType.Gondola]:   '🚠',
  [VehicleType.Funicular]: '🚟',
  [VehicleType.Unknown]:   '🚐',
}
