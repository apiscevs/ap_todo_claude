import { InMemoryCache, ApolloClientOptions } from '@apollo/client/core';
import { HttpLink } from 'apollo-angular/http';
import { inject } from '@angular/core';

export function createApollo(): ApolloClientOptions {
  const httpLink = inject(HttpLink);

  return {
    link: httpLink.create({ uri: '/graphql', withCredentials: true }),
    cache: new InMemoryCache({
      typePolicies: {
        Query: {
          fields: {
            todos: {
              merge(existing = [], incoming) {
                return incoming;
              }
            }
          }
        }
      }
    }),
    defaultOptions: {
      watchQuery: {
        errorPolicy: 'all',
        fetchPolicy: 'cache-and-network'
      }
    }
  };
}
