/** תואם ל-MyRequestDto שחוזר מה-API. שים לב: אין כאן ת"ז. */
export interface MyRequest {
  requestId: string;
  serviceName: string;
  authorityName: string;
  status: string;
  actionRequired: boolean;
  isOverdue: boolean;
  /** dd/MM/yyyy */
  submittedAt: string;
  /** dd/MM/yyyy */
  lastUpdate: string;
}

export interface MyRequestsResponse {
  requests: MyRequest[];
}
