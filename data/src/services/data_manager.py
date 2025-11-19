import polars as pl
import os
from typing import Dict, Optional, List, Any
from pathlib import Path


class Sheet:
    """Represents a sheet in a Group (Excel/CSV file)"""
    
    def __init__(self, sheet_id: str, sheet_name: str, file_path: str, 
                 sheet_index: Optional[int] = None, is_csv: bool = False):
        self.sheet_id = sheet_id
        self.sheet_name = sheet_name
        self.file_path = file_path
        self.sheet_index = sheet_index
        self.is_csv = is_csv
        
        # Metadata (computed once)
        self._columns: Optional[List[str]] = None
        self._num_rows: Optional[int] = None
        self._num_cols: Optional[int] = None
        self._start_row: int = 0
        self._start_col: int = 0
        
        # Initialize metadata
        self._initialize_metadata()
    
    def _initialize_metadata(self):
        """Initialize metadata by reading file header only (lazy approach)"""
        try:
            if self.is_csv:
                # For CSV, use scan_csv for lazy loading
                lf = pl.scan_csv(self.file_path, infer_schema_length=1000)
                # Collect only first few rows to detect table position
                df_sample = lf.head(20).collect()
            else:
                # For Excel, read only the specific sheet with limited rows
                df_sample = pl.read_excel(
                    self.file_path,
                    sheet_name=self.sheet_name,
                    read_csv_options={"infer_schema_length": 1000}
                ).head(20)
            
            # Detect table position
            self._detect_and_set_metadata(df_sample)
            
        except Exception as e:
            print(f"Error initializing metadata for sheet '{self.sheet_name}': {str(e)}")
            self._columns = []
            self._num_rows = 0
            self._num_cols = 0
    
    def _detect_and_set_metadata(self, df_sample: pl.DataFrame):
        """Detect table position and set metadata from sample"""
        if len(df_sample) == 0:
            self._columns = []
            self._num_rows = 0
            self._num_cols = 0
            return
        
        # Find header row (row with most non-null values)
        max_non_null = 0
        best_row_idx = 0
        
        for idx in range(min(10, len(df_sample))):
            row = df_sample.row(idx)
            non_null_count = sum(1 for val in row if val is not None and str(val).strip() != '')
            
            if non_null_count > max_non_null:
                max_non_null = non_null_count
                best_row_idx = idx
        
        self._start_row = best_row_idx
        
        # Get column names
        if best_row_idx == 0:
            self._columns = list(df_sample.columns)
        else:
            self._columns = [
                str(val) if val is not None else f"column_{i}" 
                for i, val in enumerate(df_sample.row(best_row_idx))
            ]
        
        # Remove empty columns
        self._columns = [col for col in self._columns if col.strip()]
        self._num_cols = len(self._columns)
        
        # Get total row count (using lazy loading)
        try:
            if self.is_csv:
                lf = pl.scan_csv(self.file_path)
                self._num_rows = lf.select(pl.count()).collect().item() - self._start_row - 1
            else:
                # For Excel, need to read full sheet to count (cached for reuse)
                df_full = pl.read_excel(
                    self.file_path,
                    sheet_name=self.sheet_name
                )
                self._num_rows = len(df_full) - self._start_row - 1
        except Exception:
            # Fallback to sample-based estimation
            self._num_rows = len(df_sample) - self._start_row - 1
        
        self._num_rows = max(0, self._num_rows if self._num_rows is not None else 0)
    
    def get_columns(self) -> List[str]:
        """Get list of column names"""
        return self._columns or []
    
    def get_num_rows(self) -> int:
        """Get total number of data rows (excluding header)"""
        return self._num_rows or 0
    
    def get_num_cols(self) -> int:
        """Get number of columns"""
        return self._num_cols or 0
    
    def get_info(self) -> Dict:
        """Get sheet metadata"""
        return {
            "sheet_id": self.sheet_id,
            "sheet_name": self.sheet_name,
            "num_rows": self.get_num_rows(),
            "num_cols": self.get_num_cols(),
            "columns": self.get_columns(),
            "start_row": self._start_row,
            "start_col": self._start_col
        }
    
    def get_row(self, row_index: int) -> Optional[Dict[str, Any]]:
        """
        Get a specific row by index (1-based, after header)
        
        Args:
            row_index: Row index (1 = first data row after header)
        """
        if row_index < 1 or row_index > self.get_num_rows():
            return None
        
        try:
            # Convert to 0-based index and skip header rows
            skip_rows = self._start_row + row_index
            
            if self.is_csv:
                df = pl.read_csv(
                    self.file_path,
                    skip_rows=skip_rows,
                    n_rows=1,
                    has_header=False,
                    new_columns=self._columns
                )
            else:
                df = pl.read_excel(
                    self.file_path,
                    sheet_name=self.sheet_name
                ).slice(skip_rows, 1)
                
                if self._start_row > 0 and self._columns:
                    df.columns = self._columns[:len(df.columns)]
            
            if len(df) > 0:
                return df.to_dicts()[0]
            return None
            
        except Exception as e:
            print(f"Error getting row {row_index}: {str(e)}")
            return None
    
    def get_rows(self, offset: int = 0, limit: Optional[int] = None) -> Dict:
        """
        Get multiple rows with pagination
        
        Args:
            offset: Starting row index (0-based)
            limit: Maximum number of rows to return
        """
        try:
            skip_rows = self._start_row + 1 + offset
            n_rows = limit if limit is not None else None
            
            if self.is_csv:
                # Use lazy loading for CSV
                lf = pl.scan_csv(self.file_path, skip_rows=skip_rows)
                if n_rows:
                    df = lf.head(n_rows).collect()
                else:
                    df = lf.collect()
            else:
                # For Excel, read and slice
                df_full = pl.read_excel(
                    self.file_path,
                    sheet_name=self.sheet_name
                )
                
                if self._start_row > 0:
                    # Extract header and data
                    # header_row = df_full.row(self._start_row)
                    df = df_full.slice(self._start_row + 1 + offset, n_rows or len(df_full))
                    if self._columns:
                        df.columns = self._columns[:len(df.columns)]
                else:
                    df = df_full.slice(offset, n_rows or len(df_full))
            
            return {
                "columns": self.get_columns(),
                "data": df.to_dicts(),
                "num_rows": len(df),
                "offset": offset,
                "total_rows": self.get_num_rows()
            }
            
        except Exception as e:
            print(f"Error getting rows: {str(e)}")
            return {
                "columns": self.get_columns(),
                "data": [],
                "num_rows": 0,
                "offset": offset,
                "total_rows": self.get_num_rows()
            }


class Group:
    """Represents a file group (CSV or Excel file)"""
    
    def __init__(self, group_id: str, file_path: str):
        self.group_id = group_id
        self.file_path = file_path
        self.file_type = Path(file_path).suffix.lower()
        self.sheets: Dict[str, Sheet] = {}
        
        # Initialize sheets
        self._initialize_sheets()
    
    def _initialize_sheets(self):
        """Initialize all sheets in the file"""
        try:
            if self.file_type == '.csv':
                # CSV has only one sheet
                sheet = Sheet(
                    sheet_id="sheet_0",
                    sheet_name="Sheet1",
                    file_path=self.file_path,
                    is_csv=True
                )
                self.sheets["sheet_0"] = sheet
                
            elif self.file_type in ['.xlsx', '.xls', '.xlsm']:
                # Get all sheet names from Excel
                import openpyxl
                workbook = openpyxl.load_workbook(self.file_path, read_only=True, data_only=True)
                sheet_names = workbook.sheetnames
                workbook.close()
                
                # Create Sheet objects for each sheet
                for idx, sheet_name in enumerate(sheet_names):
                    sheet_id = f"sheet_{idx}"
                    sheet = Sheet(
                        sheet_id=sheet_id,
                        sheet_name=sheet_name,
                        file_path=self.file_path,
                        sheet_index=idx,
                        is_csv=False
                    )
                    self.sheets[sheet_id] = sheet
            else:
                raise ValueError(f"Unsupported file format: {self.file_type}")
                
        except Exception as e:
            raise Exception(f"Error initializing sheets: {str(e)}")
    
    def get_sheet(self, sheet_id: str) -> Optional[Sheet]:
        """Get a specific sheet by ID"""
        return self.sheets.get(sheet_id)
    
    def get_sheet_ids(self) -> List[Dict]:
        """Get list of all sheet IDs and basic info"""
        return [
            {
                "sheet_id": sheet_id,
                "sheet_name": sheet.sheet_name,
                "num_rows": sheet.get_num_rows(),
                "num_cols": sheet.get_num_cols()
            }
            for sheet_id, sheet in self.sheets.items()
        ]
    
    def get_info(self) -> Dict:
        """Get group metadata"""
        return {
            "group_id": self.group_id,
            "file_path": self.file_path,
            "file_type": self.file_type,
            "num_sheets": len(self.sheets),
            "sheets": list(self.sheets.keys())
        }


class DataManager:
    def __init__(self):
        self.groups: Dict[str, Group] = {}
        
    def load_file(self, file_path: str, group_id: Optional[str] = None) -> Dict:
        """
        Load CSV or Excel file as a Group
        
        Args:
            file_path: Path to the file
            group_id: Custom ID for the group (if not provided, will use filename)
            
        Returns:
            Dict containing loaded group information
        """
        if not os.path.exists(file_path):
            raise FileNotFoundError(f"File does not exist: {file_path}")
        
        # Create group_id if not provided
        if group_id is None:
            group_id = Path(file_path).stem
        
        try:
            # Create Group object (lazy loading)
            group = Group(group_id, file_path)
            self.groups[group_id] = group
            
            return {
                "success": True,
                "group_id": group_id,
                "file_type": group.file_type,
                "num_sheets": len(group.sheets),
                "sheets": list(group.sheets.keys())
            }
            
        except Exception as e:
            raise Exception(f"Error loading file: {str(e)}")
    
    def unload_file(self, group_id: str) -> bool:
        """Unload group from system"""
        if group_id in self.groups:
            del self.groups[group_id]
            return True
        return False
    
    def get_sheet_ids(self, group_id: str) -> Optional[List[Dict]]:
        """Get list of sheet IDs and names in a group"""
        group = self.groups.get(group_id)
        if group is None:
            return None
        return group.get_sheet_ids()
    
    def get_columns(self, group_id: str, sheet_id: str) -> Optional[List[str]]:
        """Get list of column names in a sheet"""
        group = self.groups.get(group_id)
        if group is None:
            return None
        
        sheet = group.get_sheet(sheet_id)
        if sheet is None:
            return None
        
        return sheet.get_columns()
    
    def get_sheet_info(self, group_id: str, sheet_id: str) -> Optional[Dict]:
        """Get detailed information of a sheet"""
        group = self.groups.get(group_id)
        if group is None:
            return None
        
        sheet = group.get_sheet(sheet_id)
        if sheet is None:
            return None
        
        return sheet.get_info()
    
    def get_row(self, group_id: str, sheet_id: str, row_index: int) -> Optional[Dict]:
        """
        Get a specific row by index
        
        Args:
            group_id: Group ID
            sheet_id: Sheet ID
            row_index: Row index (1-based, 1 = first data row after header)
        """
        group = self.groups.get(group_id)
        if group is None:
            return None
        
        sheet = group.get_sheet(sheet_id)
        if sheet is None:
            return None
        
        return sheet.get_row(row_index)
    
    def get_data(self, group_id: str, sheet_id: str, 
                 offset: int = 0, limit: Optional[int] = None) -> Optional[Dict]:
        """
        Get sheet data with pagination
        
        Args:
            group_id: Group ID
            sheet_id: Sheet ID
            offset: Starting row index (0-based)
            limit: Maximum number of rows to return (None = all remaining)
        """
        group = self.groups.get(group_id)
        if group is None:
            return None
        
        sheet = group.get_sheet(sheet_id)
        if sheet is None:
            return None
        
        return sheet.get_rows(offset, limit)
    
    def get_loaded_files(self) -> List[Dict]:
        """Get list of all loaded groups"""
        return [
            {
                "group_id": group_id,
                "file_path": group.file_path,
                "file_type": group.file_type,
                "num_sheets": len(group.sheets)
            }
            for group_id, group in self.groups.items()
        ]


# Singleton instance
data_manager = DataManager()
