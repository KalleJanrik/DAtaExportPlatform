export interface ExportLogDto {
  appId: string;
  fileName: string;
  recordCount: number;
  fileSizeBytes: number;
  status: 'Success' | 'Failed';
  exportedAt: string;
  errorMessage?: string;
}

export interface PipelineRunDto {
  id: number;
  startedAt: string;
  finishedAt?: string;
  status: 'Running' | 'Success' | 'PartialFailure' | 'Failed';
  errorMessage?: string;
  exportLogs: ExportLogDto[];
}

export interface ArchiveSummaryDto {
  appId: string;
  dayCount: number;
  fileCount: number;
  latestDay?: string;
}

export interface ArchivedFileDto {
  fileName: string;
  sizeBytes: number;
}

export interface DayGroupDto {
  day: string;
  files: ArchivedFileDto[];
}

export interface ArchiveJobDto {
  appId: string;
  days: DayGroupDto[];
}
