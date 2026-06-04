import React from 'react';

interface UserAvatarProps {
  user: {
    imageUrl?: string;
    firstName: string;
    lastName: string;
  };
  size?: number;
  className?: string;
}

export const UserAvatar: React.FC<UserAvatarProps> = ({ user, size = 32, className = '' }) => {
  if (user.imageUrl) {
    return (
      <img
        src={user.imageUrl}
        alt={`${user.firstName} ${user.lastName}`}
        className={`rounded-circle ${className}`}
        style={{ width: size, height: size, objectFit: 'cover' }}
      />
    );
  }

  return (
    <div
      className={`rounded-circle d-flex align-items-center justify-content-center ${className}`}
      style={{
        width: size,
        height: size,
        backgroundColor: '#e9ecef',
        fontSize: size * 0.4,
        fontWeight: 'bold',
        color: '#6c757d',
      }}
    >
      {user.firstName.charAt(0)}{user.lastName.charAt(0)}
    </div>
  );
};
