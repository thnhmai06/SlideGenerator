import React, { useState, useRef, useEffect } from 'react'
import '../styles/ShapeSelector.css'

interface Shape {
  id: string
  name: string
  preview: string
}

interface ShapeSelectorProps {
  shapes: Shape[]
  value: string
  onChange: (shapeId: string) => void
  placeholder?: string
}

const ShapeSelector: React.FC<ShapeSelectorProps> = ({ 
  shapes, 
  value, 
  onChange, 
  placeholder = 'Chọn shape...' 
}) => {
  const [isOpen, setIsOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)

  const selectedShape = shapes.find(s => s.id === value)

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside)
    }

    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isOpen])

  const handleSelect = (shapeId: string) => {
    onChange(shapeId)
    setIsOpen(false)
  }

  return (
    <div className="shape-selector" ref={dropdownRef}>
      <div 
        className="shape-selector-trigger" 
        onClick={() => setIsOpen(!isOpen)}
      >
        {selectedShape ? (
          <div className="shape-option-content">
            <img 
              src={selectedShape.preview} 
              alt={selectedShape.name}
              className="shape-preview-small"
            />
            <span className="shape-name">{selectedShape.name}</span>
            <span className="shape-id">{selectedShape.id}</span>
          </div>
        ) : (
          <span className="shape-placeholder">{placeholder}</span>
        )}
        <span className="dropdown-arrow">▼</span>
      </div>

      {isOpen && (
        <div className="shape-dropdown">
          {shapes.length === 0 ? (
            <div className="shape-option-empty">Không có shape nào</div>
          ) : (
            shapes.map(shape => (
              <div
                key={shape.id}
                className={`shape-option ${shape.id === value ? 'selected' : ''}`}
                onClick={() => handleSelect(shape.id)}
              >
                <img 
                  src={shape.preview} 
                  alt={shape.name}
                  className="shape-preview"
                />
                <span className="shape-name">{shape.name}</span>
                <span className="shape-id">{shape.id}</span>
              </div>
            ))
          )}
        </div>
      )}
    </div>
  )
}

export default ShapeSelector
