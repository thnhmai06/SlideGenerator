import React, { useState, useRef, useEffect } from 'react'
import '../styles/TagInput.css'

interface TagInputProps {
  value: string[]
  onChange: (tags: string[]) => void
  suggestions: string[]
  placeholder?: string
}

const TagInput: React.FC<TagInputProps> = ({ value, onChange, suggestions, placeholder }) => {
  const [inputValue, setInputValue] = useState('')
  const [filteredSuggestions, setFilteredSuggestions] = useState<string[]>([])
  const [showSuggestions, setShowSuggestions] = useState(false)
  const [selectedSuggestionIndex, setSelectedSuggestionIndex] = useState(-1)
  const inputRef = useRef<HTMLInputElement>(null)
  const dropdownRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const filtered = suggestions.filter(s => !value.includes(s))
    
    if (inputValue) {
      const matched = filtered.filter(s => 
        s.includes(inputValue)
      )
      setFilteredSuggestions(matched)
    } else {
      setFilteredSuggestions(filtered)
    }
  }, [inputValue, suggestions, value])

const addTag = (tag: string) => {
    const suggestionExists = suggestions.some(s => s === tag)
    if (tag && !value.includes(tag) && suggestionExists) {
        onChange([...value, tag])
        setInputValue('')
        setShowSuggestions(true)
        setSelectedSuggestionIndex(-1)
    }
}

  const removeTag = (tagToRemove: string) => {
    onChange(value.filter(tag => tag !== tagToRemove))
  }

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.value
    
    // Check if user typed comma
    if (newValue.endsWith(',')) {
      const tag = newValue.slice(0, -1)
      if (tag) {
        addTag(tag)
      }
      return
    }
    
    setInputValue(newValue)
    setSelectedSuggestionIndex(-1)
    setShowSuggestions(true)
  }

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault()
      if (selectedSuggestionIndex >= 0 && filteredSuggestions[selectedSuggestionIndex]) {
        addTag(filteredSuggestions[selectedSuggestionIndex])
      } else if (inputValue) {
        addTag(inputValue)
      }
    } else if (e.key === 'Backspace' && !inputValue && value.length > 0) {
      // Remove last tag when backspace on empty input
      removeTag(value[value.length - 1])
    } else if (e.key === 'ArrowDown') {
      e.preventDefault()
      if (showSuggestions && filteredSuggestions.length > 0) {
        setSelectedSuggestionIndex(prev => 
          prev < filteredSuggestions.length - 1 ? prev + 1 : prev
        )
      }
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      if (showSuggestions && filteredSuggestions.length > 0) {
        setSelectedSuggestionIndex(prev => prev > 0 ? prev - 1 : -1)
      }
    } else if (e.key === 'Escape') {
      setShowSuggestions(false)
      setSelectedSuggestionIndex(-1)
    }
  }

  const handleInputFocus = () => {
    setShowSuggestions(true)
  }

  const handleInputBlur = () => {
    // Delay to allow click on suggestion
    setTimeout(() => {
      setShowSuggestions(false)
      setSelectedSuggestionIndex(-1)
    }, 200)
  }

  return (
    <div className="tag-input-container">
      <div 
        className="tag-input-wrapper"
        onClick={() => inputRef.current?.focus()}
      >
        {value.map((tag, index) => (
          <div key={index} className="tag-item">
            <span className="tag-text">{tag}</span>
            <button
              type="button"
              className="tag-remove"
              onClick={() => removeTag(tag)}
              title="Remove"
            >
              ×
            </button>
          </div>
        ))}
        <input
          ref={inputRef}
          type="text"
          className="tag-input-field"
          value={inputValue}
          onChange={handleInputChange}
          onKeyDown={handleKeyDown}
          onFocus={handleInputFocus}
          onBlur={handleInputBlur}
          placeholder={value.length === 0 ? placeholder : ''}
        />
      </div>
      
      {showSuggestions && filteredSuggestions.length > 0 && (
        <div ref={dropdownRef} className="tag-suggestions">
          {filteredSuggestions.slice(0, 10).map((suggestion, index) => (
            <div
              key={suggestion}
              className={`tag-suggestion-item ${index === selectedSuggestionIndex ? 'selected' : ''}`}
              onMouseDown={(e) => {
                e.preventDefault()
                addTag(suggestion)
              }}
            >
              {suggestion}
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

export default TagInput
