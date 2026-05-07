import React, { memo, useCallback, useMemo, useRef, useState } from 'react';
import './TagInput.css';

/** Props for {@link TagInput}. */
interface TagInputProps {
	/** Current selected tags. */
	value: string[];
	/** Callback when tags change. */
	onChange: (tags: string[]) => void;
	/** Available suggestions for autocomplete. */
	suggestions: string[];
	/** Placeholder text when empty. */
	placeholder?: string;
}

/**
 * Multi-select tag input with autocomplete suggestions.
 *
 * @remarks
 * Supports keyboard navigation (Arrow keys, Enter, Escape, Backspace).
 * Tags can be added by selecting from suggestions or typing with comma.
 */
const TagInput: React.FC<TagInputProps> = memo(({ value, onChange, suggestions, placeholder }) => {
	const [inputValue, setInputValue] = useState('');
	const [showSuggestions, setShowSuggestions] = useState(false);
	const [selectedSuggestionIndex, setSelectedSuggestionIndex] = useState(-1);
	const inputRef = useRef<HTMLInputElement>(null);
	const dropdownRef = useRef<HTMLDivElement>(null);

	const filteredSuggestions = useMemo(() => {
		const filtered = suggestions.filter((suggestion) => !value.includes(suggestion));
		if (!inputValue) {
			return filtered;
		}
		return filtered.filter((suggestion) => suggestion.includes(inputValue));
	}, [inputValue, suggestions, value]);

	const addTag = useCallback(
		(tag: string) => {
			const suggestionExists = suggestions.some((s) => s === tag);
			if (tag && !value.includes(tag) && suggestionExists) {
				onChange([...value, tag]);
				setInputValue('');
				setShowSuggestions(true);
				setSelectedSuggestionIndex(-1);
			}
		},
		[onChange, suggestions, value],
	);

	const removeTag = useCallback(
		(tagToRemove: string) => {
			onChange(value.filter((tag) => tag !== tagToRemove));
		},
		[onChange, value],
	);

	const handleInputChange = useCallback(
		(e: React.ChangeEvent<HTMLInputElement>) => {
			const newValue = e.target.value;

			if (newValue.endsWith(',')) {
				const tag = newValue.slice(0, -1);
				if (tag) {
					addTag(tag);
				}
				return;
			}

			setInputValue(newValue);
			setSelectedSuggestionIndex(-1);
			setShowSuggestions(true);
		},
		[addTag],
	);

	const handleKeyDown = useCallback(
		(e: React.KeyboardEvent<HTMLInputElement>) => {
			if (e.key === 'Enter') {
				e.preventDefault();
				if (selectedSuggestionIndex >= 0 && filteredSuggestions[selectedSuggestionIndex]) {
					addTag(filteredSuggestions[selectedSuggestionIndex]);
				} else if (inputValue) {
					addTag(inputValue);
				}
			} else if (e.key === 'Backspace' && !inputValue && value.length > 0) {
				removeTag(value[value.length - 1]);
			} else if (e.key === 'ArrowDown') {
				e.preventDefault();
				if (showSuggestions && filteredSuggestions.length > 0) {
					setSelectedSuggestionIndex((prev) =>
						prev < filteredSuggestions.length - 1 ? prev + 1 : prev,
					);
				}
			} else if (e.key === 'ArrowUp') {
				e.preventDefault();
				if (showSuggestions && filteredSuggestions.length > 0) {
					setSelectedSuggestionIndex((prev) => (prev > 0 ? prev - 1 : -1));
				}
			} else if (e.key === 'Escape') {
				setShowSuggestions(false);
				setSelectedSuggestionIndex(-1);
			}
		},
		[
			addTag,
			filteredSuggestions,
			inputValue,
			removeTag,
			selectedSuggestionIndex,
			showSuggestions,
			value,
		],
	);

	const handleInputFocus = useCallback(() => {
		setShowSuggestions(true);
	}, []);

	const handleInputBlur = useCallback(() => {
		setTimeout(() => {
			setShowSuggestions(false);
			setSelectedSuggestionIndex(-1);
		}, 200);
	}, []);

	return (
		<div
			className={`tag-input-container ${showSuggestions && filteredSuggestions.length > 0 ? 'is-active' : ''}`}
		>
			<div className="tag-input-wrapper" onClick={() => inputRef.current?.focus()}>
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
					{filteredSuggestions.map((suggestion, index) => (
						<div
							key={suggestion}
							className={`tag-suggestion-item ${index === selectedSuggestionIndex ? 'selected' : ''}`}
							onMouseDown={(e) => {
								e.preventDefault();
								addTag(suggestion);
							}}
						>
							{suggestion}
						</div>
					))}
				</div>
			)}
		</div>
	);
});

TagInput.displayName = 'TagInput';

export default TagInput;
