import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { act } from 'react'
import { FilterBar } from '../components/FilterBar'
import { useVehicleStore } from '../stores/vehicleStore'
import { VehicleType } from '../types/vehicle'

describe('FilterBar', () => {
  beforeEach(() => {
    // Reset to all filters active
    act(() => {
      const { toggleFilter, activeFilters } = useVehicleStore.getState()
      // Ensure all main types are active
      if (!activeFilters.has(VehicleType.Bus)) toggleFilter(VehicleType.Bus)
      if (!activeFilters.has(VehicleType.Train)) toggleFilter(VehicleType.Train)
    })
  })

  it('renders filter buttons for main vehicle types', () => {
    render(<FilterBar />)
    expect(screen.getByRole('button', { name: /toggle bus/i })).toBeTruthy()
    expect(screen.getByRole('button', { name: /toggle train/i })).toBeTruthy()
    expect(screen.getByRole('button', { name: /toggle ferry/i })).toBeTruthy()
  })

  it('buttons are initially pressed (active)', () => {
    render(<FilterBar />)
    const busBtn = screen.getByRole('button', { name: /toggle bus/i })
    expect(busBtn.getAttribute('aria-pressed')).toBe('true')
  })

  it('toggling Bus removes it from active filters', () => {
    render(<FilterBar />)
    fireEvent.click(screen.getByRole('button', { name: /toggle bus/i }))
    expect(useVehicleStore.getState().activeFilters.has(VehicleType.Bus)).toBe(false)
  })

  it('toggling Bus twice restores it', () => {
    render(<FilterBar />)
    fireEvent.click(screen.getByRole('button', { name: /toggle bus/i }))
    fireEvent.click(screen.getByRole('button', { name: /toggle bus/i }))
    expect(useVehicleStore.getState().activeFilters.has(VehicleType.Bus)).toBe(true)
  })
})
