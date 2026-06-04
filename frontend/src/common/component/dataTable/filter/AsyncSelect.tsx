import React, { useState, useEffect, useRef } from 'react';
import { Form, Dropdown, Spinner } from 'react-bootstrap';
import { X } from 'react-bootstrap-icons';

export interface AsyncSelectProps<T> {
  value?: string | number;
  onChange: (value: string | number | undefined) => void;
  loadOptions: (search: string) => Promise<T[]>;
  getOptionLabel: (option: T) => string;
  getOptionValue: (option: T) => string | number;
  getOptionSubtitle?: (option: T) => string;
  placeholder?: string;
  className?: string;
}

export function AsyncSelect<T>({
  value,
  onChange,
  loadOptions,
  getOptionLabel,
  getOptionValue,
  getOptionSubtitle,
  placeholder = "Select...",
  className
}: AsyncSelectProps<T>) {
  const [options, setOptions] = useState<T[]>([]);
  const [loading, setLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedOption, setSelectedOption] = useState<T | undefined>(undefined);
  const [isOpen, setIsOpen] = useState(false);
  const lastFetchedTerm = useRef<string | null>(null);


  useEffect(() => {
    if (isOpen) {
      const timer = setTimeout(() => {
        if (searchTerm !== lastFetchedTerm.current) {
          fetchOptions(searchTerm);

        }
      }, 500);

      return () => clearTimeout(timer);
    }
  }, [searchTerm, isOpen]);

  // Update selected option when value changes or options are loaded
  useEffect(() => {
    if (value !== undefined && value !== null) {
      const found = options.find(opt => getOptionValue(opt) === value);
      if (found) {
        setSelectedOption(found);
      } else if (!selectedOption) {
        if (options.length === 0 && !loading) {
          fetchOptions("");
        }
      }
    } else {
      setSelectedOption(undefined);
    }
  }, [value, options]);

  const fetchOptions = async (search: string) => {
    lastFetchedTerm.current = search;
    setLoading(true);
    try {
      const results = await loadOptions(search);
      setOptions(results);
    } catch (error) {
      console.error("Failed to load options", error);
      setOptions([]);
      lastFetchedTerm.current = null;
    } finally {
      setLoading(false);
    }
  };

  const handleSelect = (option: T) => {
    const val = getOptionValue(option);
    setSelectedOption(option);
    onChange(val);
    setIsOpen(false);
  };

  const handleClear = (e: React.MouseEvent) => {
    e.stopPropagation();
    setSelectedOption(undefined);
    onChange(undefined);
    setSearchTerm('');
  };

  // Custom Toggle
  const CustomToggle = React.forwardRef<HTMLDivElement, { onClick: (e: React.MouseEvent) => void; children?: React.ReactNode }>(
    ({ onClick }, ref) => (
      <div
        ref={ref}
        onClick={(e) => {
          e.preventDefault();
          onClick(e);
        }}
        className={`form-control d-flex justify-content-between align-items-center cursor-pointer ${className}`}
        style={{ cursor: 'pointer', minWidth: '250px' }}
      >
        <span className="text-truncate">
          {selectedOption ? getOptionLabel(selectedOption) : placeholder}
        </span>
        {selectedOption ? (
          <X size={20} onClick={handleClear} className="text-muted" />
        ) : (
          <span className="dropdown-toggle-icon"></span>
        )}
      </div>
    )
  );

  CustomToggle.displayName = 'CustomToggle';

  return (
    <Dropdown show={isOpen} onToggle={(show) => setIsOpen(show)}>
      <Dropdown.Toggle as={CustomToggle} />

      <Dropdown.Menu className="w-100" style={{ minWidth: '250px' }}>
        <div className="p-2">
          <Form.Control
            autoFocus
            placeholder="Search..."
            style={{ maxHeight: '300px', overflowY: 'auto' }}
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>

        <div style={{ maxHeight: '300px', overflowY: 'auto' }}>
          {loading ? (
            <div className="text-center p-2">
              <Spinner animation="border" size="sm" />
            </div>
          ) : options.length > 0 ? (
            options.map((option) => {
              const optValue = getOptionValue(option);
              return (
                <Dropdown.Item
                  key={optValue}
                  onClick={() => handleSelect(option)}
                  active={value === optValue}
                >
                  <div className="fw-bold">{getOptionLabel(option)}</div>
                  {getOptionSubtitle && <div className="small ">{getOptionSubtitle(option)}</div>}
                </Dropdown.Item>
              );
            })
          ) : (
            <div className="text-center text-muted p-2">No results found</div>
          )}
        </div>
      </Dropdown.Menu>
    </Dropdown>
  );
}
