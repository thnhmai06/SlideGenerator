# Deployment

Vietnamese version: [Vietnamese](../vi/deployment.md)

## Table of contents

1. [Overview](#overview)
2. [Configuration](#configuration)
3. [Notes](#notes)

## Overview

The backend is an ASP.NET Core app hosted by `SlideGenerator.Presentation`.

## Configuration

Before deploying, configure:

- Server host/port
- Hangfire SQLite database path
- Hangfire worker count (max concurrent jobs)

See: [Configuration](configuration.md)

## Notes

- Ensure output folders exist and are writable.
- Ensure the Hangfire database directory is writable.

