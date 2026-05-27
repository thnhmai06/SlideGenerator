# Cryptography Module

The **SlideGenerator.Cryptography** module provides security services for sensitive data.

## Responsibility
- Encrypting cloud credentials.
- Generating secure machine-specific keys.
- Managing a global file hash registry.

## Security Standard
- **Algorithm**: AES-256 (GCM) for authenticated encryption.
- **Key Derivation**: Machine-locked keys derived via PBKDF2.

## File Hash Registry
To prevent redundant processing, the system uses the `IHashPathRegistry` to map long absolute paths to short, 7-character hash identifiers (e.g., for naming temporary folders).
