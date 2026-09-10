import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SearchRequest } from '../models/search-request';
import { SearchResponse } from '../models/search-response';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class FlightService {
  private readonly httpClient = inject(HttpClient);

  search(request: SearchRequest): Observable<SearchResponse> {
    return this.httpClient.post<SearchResponse>(
      `${environment.apiUrl}/flights/search`,
      request
    );
  }
}
