# Product Overview

KAPE Viewer is a Windows desktop application for Digital Forensics and Incident Response (DFIR) engineers. It provides efficient browsing and analysis of KAPE/!EZParser CSV outputs.

## Core Functionality

- Browse CSV files organized by artifact groups (ProgramExecution, EventLogs, etc.)
- View individual CSV files in a table with sorting and filtering
- Merge all CSV files into a chronological timeline
- Filter data by time range, source group, and text search
- Toggle between UTC and local time display
- Export filtered data to CSV
- Copy selected rows to clipboard

## Target Users

DFIR engineers analyzing forensic artifacts from KAPE (Kroll Artifact Parser and Extractor) output folders.

## Key Design Goals

- Performance: Handle large CSV files (100K+ rows) efficiently using streaming and virtualization
- Usability: Familiar interface similar to Timeline Explorer
- Reliability: Gracefully handle malformed CSV files and various timestamp formats
