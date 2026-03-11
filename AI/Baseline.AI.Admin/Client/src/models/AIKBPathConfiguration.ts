import { AIKBContentType } from './AIKBContentType';

export interface AIKBPathConfiguration {
  identifier: number | null;
  channelName: string;
  channelDisplayName: string;
  includePattern: string;
  excludePattern: string | null;
  contentTypes: AIKBContentType[];
  priority: number;
  includeChildren: boolean;
}
