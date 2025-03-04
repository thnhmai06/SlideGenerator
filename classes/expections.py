from requests import Response

class RateLimitException(Exception):
    """Exception đại diện 429 Too Many Requests"""
    
    @staticmethod
    def is_rate_limited(response: Response) -> bool:
        """
        Kiểm tra xem response có bị rate limit hay không.
        
        Args:
            response (Response): Response từ requests
            
        Returns:
            bool: True nếu bị rate limit, False nếu không
        """
        return response.status_code == 429
    
    pass