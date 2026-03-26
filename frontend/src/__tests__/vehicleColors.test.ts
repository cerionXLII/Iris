import { describe, it, expect } from 'vitest'
import { getVehicleColor, VEHICLE_TYPE_LABELS, VEHICLE_TYPE_ICONS } from '../utils/vehicleColors'
import { VehicleType } from '../types/vehicle'

describe('vehicleColors', () => {
  it('getVehicleColor returns a 4-element RGBA tuple for each type', () => {
    const types = [
      VehicleType.Bus, VehicleType.Train, VehicleType.Metro,
      VehicleType.Tram, VehicleType.Ferry, VehicleType.Unknown,
    ]
    for (const type of types) {
      const color = getVehicleColor(type)
      expect(color).toHaveLength(4)
      color.forEach((c) => {
        expect(c).toBeGreaterThanOrEqual(0)
        expect(c).toBeLessThanOrEqual(255)
      })
    }
  })

  it('getVehicleColor falls back for unknown type', () => {
    const color = getVehicleColor(99 as VehicleType)
    expect(color).toHaveLength(4)
  })

  it('VEHICLE_TYPE_LABELS has a label for every type', () => {
    const types = Object.values(VehicleType).filter((v) => typeof v === 'number') as VehicleType[]
    for (const type of types) {
      expect(VEHICLE_TYPE_LABELS[type]).toBeTruthy()
    }
  })

  it('VEHICLE_TYPE_ICONS has an icon for every type', () => {
    const types = Object.values(VehicleType).filter((v) => typeof v === 'number') as VehicleType[]
    for (const type of types) {
      expect(VEHICLE_TYPE_ICONS[type]).toBeTruthy()
    }
  })

  it('Bus color is blue-ish', () => {
    const [red, green, blue] = getVehicleColor(VehicleType.Bus)
    expect(blue).toBeGreaterThan(red) // blue dominant
    expect(green).toBeGreaterThan(red)
  })

  it('Ferry color is cyan-ish', () => {
    const [, green, blue] = getVehicleColor(VehicleType.Ferry)
    expect(blue).toBeGreaterThan(50)
    expect(green).toBeGreaterThan(50)
  })
})
