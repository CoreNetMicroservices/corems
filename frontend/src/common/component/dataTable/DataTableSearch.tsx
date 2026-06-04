import React, { useRef } from 'react';
import { Form } from 'react-bootstrap';

interface DataTableSearchProps {
  placeholder?: string;
  defaultValue?: string;
  onSearch: (search: string) => void;
}

export const DataTableSearch: React.FC<DataTableSearchProps> = ({
  placeholder = "Search...",
  defaultValue = "",
  onSearch
}) => {
  const searchRef = useRef<HTMLInputElement>(null);

  const handleSearch = () => {
    const searchValue = searchRef.current?.value || '';
    onSearch(searchValue);
  };

  const handleKeyPress = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') handleSearch();
  };

  return (
    <Form.Control
      ref={searchRef}
      type="text"
      placeholder={placeholder}
      defaultValue={defaultValue}
      onKeyDown={handleKeyPress}
      onBlur={handleSearch}
    />
  );
};