class IndexOutOfRange(IndexError):
    """Raised when an index is out of the valid range_."""

    def __init__(self, index: int, range_: tuple[int, int] = None):
        super().__init__(f"Index {index} is out of range {range_}.")
