import { FormComponentProps } from '@kentico/xperience-admin-base';
import { AIKBPathConfiguration } from './AIKBPathConfiguration';
import { AIKBContentType } from './AIKBContentType';
import { AIKBChannel } from './AIKBChannel';

export interface AIKBPathConfigProps extends FormComponentProps {
  value: AIKBPathConfiguration[];
  possibleContentTypeItems: AIKBContentType[] | null;
  possibleChannels: AIKBChannel[] | null;
}
