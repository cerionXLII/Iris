import { useCallback, useMemo, useRef, useState } from 'react'
import Map, { NavigationControl } from 'react-map-gl/maplibre'
import { DeckGL } from '@deck.gl/react'
import { ScatterplotLayer } from 'deck.gl'
import type { PickingInfo } from 'deck.gl'
import 'maplibre-gl/dist/maplibre-gl.css'
import { useVehicleStore } from '../stores/vehicleStore'
import type { VehicleSnapshot } from '../types/vehicle'
import { getVehicleColor } from '../utils/vehicleColors'

const INITIAL_VIEW_STATE = {
  longitude: 17.0,
  latitude: 62.5,
  zoom: 5,
  pitch: 0,
  bearing: 0,
}

// CartoDB Dark Matter — free, no API key needed
const MAP_STYLE = {
  version: 8,
  sources: {
    carto: {
      type: 'raster',
      tiles: [
        'https://a.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}@2x.png',
        'https://b.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}@2x.png',
        'https://c.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}@2x.png',
      ],
      tileSize: 256,
      attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors © <a href="https://carto.com/">CARTO</a>',
      maxzoom: 19,
    },
  },
  layers: [
    {
      id: 'carto-dark',
      type: 'raster',
      source: 'carto',
    },
  ],
}

interface IrisMapProps {
  onVehicleSelect: (vehicle: VehicleSnapshot | null) => void
  selectedVehicleId: string | null
}

export function IrisMap({ onVehicleSelect, selectedVehicleId }: IrisMapProps) {
  const getFilteredVehicles = useVehicleStore((s) => s.getFilteredVehicles)
  const [viewState, setViewState] = useState(INITIAL_VIEW_STATE)
  const isDragging = useRef(false)

  const vehicles = useMemo(() => getFilteredVehicles(), [getFilteredVehicles])

  const vehicleLayer = useMemo(
    () =>
      new ScatterplotLayer<VehicleSnapshot>({
        id: 'vehicles',
        data: vehicles,
        getPosition: (v) => [v.longitude, v.latitude],
        getColor: (v) => getVehicleColor(v.vehicleType),
        getRadius: (v) => (v.vehicleId === selectedVehicleId ? 400 : 260),
        radiusMinPixels: 4,
        radiusMaxPixels: 16,
        radiusUnits: 'meters',
        pickable: true,
        stroked: true,
        getLineColor: (v) =>
          v.vehicleId === selectedVehicleId
            ? [255, 255, 255, 200]
            : [0, 0, 0, 60],
        lineWidthMinPixels: 1,
        transitions: {
          getPosition: { duration: 2500 },
          getColor: 200,
          getRadius: 200,
        },
        updateTriggers: {
          getRadius: [selectedVehicleId],
          getLineColor: [selectedVehicleId],
        },
      }),
    [vehicles, selectedVehicleId]
  )

  const handleClick = useCallback(
    (info: PickingInfo) => {
      if (isDragging.current) return
      onVehicleSelect((info.object as VehicleSnapshot | undefined) ?? null)
    },
    [onVehicleSelect]
  )

  return (
    <DeckGL
      viewState={viewState}
      onViewStateChange={({ viewState: vs }) =>
        setViewState(vs as typeof INITIAL_VIEW_STATE)
      }
      controller={true}
      layers={[vehicleLayer]}
      onClick={handleClick}
      onDragStart={() => { isDragging.current = true }}
      onDragEnd={() => { setTimeout(() => { isDragging.current = false }, 50) }}
      getCursor={({ isHovering }) => (isHovering ? 'pointer' : 'grab')}
    >
      <Map mapStyle={MAP_STYLE as never} reuseMaps>
        <NavigationControl position="top-right" />
      </Map>
    </DeckGL>
  )
}
