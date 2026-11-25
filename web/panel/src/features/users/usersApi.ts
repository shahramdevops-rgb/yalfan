import { api } from '../../shared/api/client';
import { unwrap } from '../../shared/api/request';
import type { components } from '../../shared/api/schema';

export type StaffUser = components['schemas']['StaffUserDetails'];
export type LoginNumber = components['schemas']['LoginNumberDetails'];
export type Role = components['schemas']['Role'];

/** The roles the panel gives; an owner account comes from the server's configuration (ADR 0026). */
export type StaffRole = Exclude<Role, 'owner'>;

export interface NewStaffUser {
  fullName: string;
  mobileNumber: string;
  role: StaffRole;
}

const at = (id: string) => ({ params: { path: { id } } });

/** The owner's users screen (ADR 0012, ADR 0026). Every change answers the whole user. */
export const usersApi = {
  list: () => unwrap(api.GET('/panel/users')),
  create: (user: NewStaffUser) => unwrap(api.POST('/panel/users', { body: user })),
  rename: (id: string, fullName: string) =>
    unwrap(api.PUT('/panel/users/{id}/name', { ...at(id), body: { fullName } })),
  changeRole: (id: string, role: StaffRole) => unwrap(api.PUT('/panel/users/{id}/role', { ...at(id), body: { role } })),
  changeMobile: (id: string, mobileNumber: string) =>
    unwrap(api.PUT('/panel/users/{id}/mobile', { ...at(id), body: { mobileNumber } })),
  setActive: (id: string, isActive: boolean) =>
    unwrap(
      isActive ? api.POST('/panel/users/{id}/activate', at(id)) : api.POST('/panel/users/{id}/deactivate', at(id)),
    ),
  setNumberActive: (id: string, numberId: string, isActive: boolean) => {
    const path = { params: { path: { id, numberId } } };
    return unwrap(
      isActive
        ? api.POST('/panel/users/{id}/numbers/{numberId}/activate', path)
        : api.POST('/panel/users/{id}/numbers/{numberId}/deactivate', path),
    );
  },
};

export const usersKey = ['users'] as const;
