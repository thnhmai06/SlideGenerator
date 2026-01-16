import React, { memo, useCallback, useMemo, useState, useRef, useEffect } from 'react';
import './ShapeSelector.css';

/** Shape data for selector options. */
export interface Shape {
	/** Unique shape identifier. */
	id: string;
	/** Display name. */
	name: string;
	/** Preview image URL or data URI. */
	preview: string;
}

/** Props for {@link ShapeSelector}. */
interface ShapeSelectorProps {
	/** Available shapes to select from. */
	shapes: Shape[];
	/** Currently selected shape ID. */
	value: string;
	/** Callback when selection changes. */
	onChange: (shapeId: string) => void;
	/** Placeholder text when no selection. */
	placeholder?: string;
}

/**
 * Dropdown selector for PowerPoint shapes with image previews.
 *
 * @remarks
 * Displays shape thumbnail, name, and ID in both trigger and dropdown.
 * Closes automatically when clicking outside.
 */
const ShapeSelector: React.FC<ShapeSelectorProps> = memo(
	({ shapes, value, onChange, placeholder = 'Chọn shape...' }) => {
		const [isOpen, setIsOpen] = useState(false);
		const dropdownRef = useRef<HTMLDivElement>(null);

		const selectedShape = useMemo(() => shapes.find((s) => s.id === value), [shapes, value]);

		useEffect(() => {
			const handleClickOutside = (event: MouseEvent) => {
				if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
					setIsOpen(false);
				}
			};

			if (isOpen) {
				document.addEventListener('mousedown', handleClickOutside);
			}

			return () => {
				document.removeEventListener('mousedown', handleClickOutside);
			};
		}, [isOpen]);

		const handleSelect = useCallback(
			(shapeId: string) => {
				onChange(shapeId);
				setIsOpen(false);
			},
			[onChange],
		);

		const toggleOpen = useCallback(() => {
			setIsOpen((prev) => !prev);
		}, []);

		return (
			<div className={`shape-selector ${isOpen ? 'is-active' : ''}`} ref={dropdownRef}>
				<div className="shape-selector-trigger" onClick={toggleOpen}>
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
							shapes.map((shape) => (
								<div
									key={shape.id}
									className={`shape-option ${shape.id === value ? 'selected' : ''}`}
									onClick={() => handleSelect(shape.id)}
								>
									<img src={shape.preview} alt={shape.name} className="shape-preview" />
									<span className="shape-name">{shape.name}</span>
									<span className="shape-id">{shape.id}</span>
								</div>
							))
						)}
					</div>
				)}
			</div>
		);
	},
);

ShapeSelector.displayName = 'ShapeSelector';

export default ShapeSelector;
