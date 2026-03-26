import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { InfoPanel } from '../components/InfoPanel'
import type { VehicleSnapshot } from '../types/vehicle'
import { VehicleType } from '../types/vehicle'

const mockVehicle: VehicleSnapshot = {
  vehicleId: 'bus-1',
  latitude: 59.3,
  longitude: 18.06,
  bearing: 45,
  speed: 15,
  vehicleType: VehicleType.Bus,
  routeShortName: '65',
  headsign: 'Tekniska Högskolan',
  agencyName: 'SL',
  delaySeconds: 120,
  timestamp: new Date().toISOString(),
}

describe('InfoPanel', () => {
  it('renders route short name', () => {
    render(<InfoPanel vehicle={mockVehicle} onClose={() => {}} />)
    expect(screen.getAllByText('65').length).toBeGreaterThan(0)
  })

  it('renders headsign/destination', () => {
    render(<InfoPanel vehicle={mockVehicle} onClose={() => {}} />)
    expect(screen.getAllByText(/Tekniska/i).length).toBeGreaterThan(0)
  })

  it('renders delay as late when positive', () => {
    render(<InfoPanel vehicle={mockVehicle} onClose={() => {}} />)
    expect(screen.getAllByText(/min late/i).length).toBeGreaterThan(0)
  })

  it('renders "On time" when delaySeconds is 0', () => {
    render(<InfoPanel vehicle={{ ...mockVehicle, delaySeconds: 0 }} onClose={() => {}} />)
    expect(screen.getAllByText('On time').length).toBeGreaterThan(0)
  })

  it('renders "On time" when delaySeconds is null', () => {
    render(<InfoPanel vehicle={{ ...mockVehicle, delaySeconds: null }} onClose={() => {}} />)
    // null delay → gray 'On time' text
    expect(screen.getAllByText('On time').length).toBeGreaterThan(0)
  })

  it('renders speed in km/h', () => {
    render(<InfoPanel vehicle={mockVehicle} onClose={() => {}} />)
    // 15 m/s * 3.6 = 54 km/h
    expect(screen.getAllByText('54 km/h').length).toBeGreaterThan(0)
  })

  it('calls onClose when close button is clicked', () => {
    const onClose = vi.fn()
    render(<InfoPanel vehicle={mockVehicle} onClose={onClose} />)
    fireEvent.click(screen.getAllByRole('button', { name: /close/i })[0])
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('calls onClose when Escape is pressed', () => {
    const onClose = vi.fn()
    render(<InfoPanel vehicle={mockVehicle} onClose={onClose} />)
    fireEvent.keyDown(window, { key: 'Escape' })
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('renders vehicle type label', () => {
    render(<InfoPanel vehicle={mockVehicle} onClose={() => {}} />)
    expect(screen.getAllByText(/Bus/i).length).toBeGreaterThan(0)
  })
})
